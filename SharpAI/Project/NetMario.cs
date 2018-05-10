using Core.Classes;
using Core.Enums;
using Core.Modifications;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;


/*############################################################################*
 *             NEAT Algorithm based on MarI/O by SethBling 2015               *
 *               https://www.youtube.com/watch?v=qv6UVOQ0F44                  *
 *                  translatet to c# by Martin Kober 2017                     *
 *############################################################################*/


namespace NeuralNet.Project
{
    public class NetMario
    {
        public static NetMain NetMain;

        //PRE-INITIALISED ORIGINALE WERTE AUS NEATEVOLVE.LUA
        public int Population = 300;                                //GRÖSSE DER POPULATION BIS ZU NÄCHSTEN GENERATION
        public const double DeltaDisjoint = 2.0;
        public const double DeltaWeights = 0.4;
        public const double DeltaThreshold = 1.0;

        public static double StaleSpecies = 15;                     //VERJÄHRUNG EINER SPECIES

        public const double MutateConnectionsChance = 0.25;
        public const double PerturbChance = 0.90;                   //CONSTANT IN POINT MUTATION
        public const double CrossoverChance = 0.75;
        public const double LinkMutationChance = 2.0;
        public const double NodeMutationChance = 0.50;
        public const double BiasMutationChance = 0.40;
        public const double StepSize = 0.1;                         //STEP SIZE IN POINT MUTATION
        public const double DisableMutationChance = 0.4;
        public const double EnableMutationChance = 0.2;

        public const int MaxNodes = 1000000;

        //TO PROOF
        private bool Reset = true, Wait;
        private int Score;
        private Dictionary<string, bool> Controller = new Dictionary<string, bool>();

        //AFTER-INITIALISED
        public newPool Pool;

        public Size BoxSize;
        public int InputSize;
        public static double[] Inputs;
        public static int InputsCount;
        public static string[] OutputKeys;
        public static double[] Outputs;
        public static int OutputsCount;
        public bool Learning = false;
        public string Status = string.Empty;
        public DateTime Start, StartRun;
        public DateTime End;
        public TimeSpan Duration, DurationRun;

        public NetMario(NetMain xNetMain)
        {
            //NET MARIO CLASS
            NetMain = xNetMain;
        }

        public void Initialize(string[] xOutputKeys, bool xLoad = false)
        {
            //ABBRUCH
            if (xOutputKeys == null)
                return;

            //PROPERTIES
            Population = NetMain.NeatPopulation.getInteger();
            StaleSpecies = NetMain.NeatStalness.getInteger();

            //INITIALIZE INPUTS AND OUTPUTS
            BoxSize = NetMain.Net.SizeNet;
            InputSize = NetMain.Net.NodesInput;
            InputsCount = InputSize + 1;
            Outputs = new double[xOutputKeys.Length]; //DEFINE OUTPUT VECTOR SIZE
            OutputKeys = xOutputKeys;
            OutputsCount = OutputKeys.Length;

            //INITIALIZE POOL
            initializePool(xLoad);
        }

        public void Run(string[] xOutputKeys = null)
        {
            //ABBRUCH
            if (!NetMain.Cam.Visible && NetMain.ToggleLearn.Checked)
            { NetMain.setConsole("Camera is off"); Learning = false; NetMain.ToggleLearn.Checked = false; return; }

            //RUN IN BACKGROUND PROCESS
            if (NetMain.ToggleLearn.Checked)
            {
                if (Pool == null) Initialize(xOutputKeys);

                NetMain.setConsole("Machine learning starts in... (hit [SPACE] to abort)");
                Mod_Process.ProcessDelay(new Action(() => { NetMain.setConsole("3..."); }), 1000);
                Mod_Process.ProcessDelay(new Action(() => { NetMain.setConsole("2..."); }), 2000);
                Mod_Process.ProcessDelay(new Action(() => { NetMain.setConsole("1..."); }), 3000);
                Mod_Process.ProcessDelay(new Action(() => { Start = DateTime.Now; Learning = true; }), 4000);
            }
            else
            {
                Learning = false;
                End = DateTime.Now;
                Duration = End - Start + Duration;
            }
        }

        public void LearningRun()
        {
            //LEARNING RUN          
            if (!Learning)
            { Status = "stopped"; return; }

            //GET SCORE
            Score = NetMain.Cam.Score; //SCORE = TIME -> T-REX RUNNING            

            //RESET GAME
            if (Reset) { NetMain.Cam.eventResetGame(); Wait = true; Status = "reset"; }
            if (Wait) { if (NetMain.Cam.Alive) { Reset = Wait = false; StartRun = DateTime.Now; } else return; }

            Status = "alive";

            //GET CURRENT SPECIES & GENOME
            newSpecies species = Pool.species[Pool.currentSpecies];
            newGenome genome = species.genomes[Pool.currentGenome];

            //CONVERT INPUTS TO OUTPUTS
            evaluateCurrent();

            //EXECUTE OUTPUTS
            foreach (string key in Controller.Keys.ToList())
                if (Controller[key]) NetMain.Cam.eventGlobalSendKeys(Array.IndexOf(Controller.Keys.ToArray(), key)); //SENDKEYS
                else NetMain.Cam.eventGlobalSendKeys(int.MinValue);

            //CHECK IS ALIVE
            if (!NetMain.Cam.Alive && !Reset)
            {
                double fitness = Score;
                if (Score > 500) //INPUT BONUS SCORE
                    fitness = fitness + 200;

                if (fitness == 0)
                    fitness = -1;

                genome.fitness = fitness;

                //MAKE BACKUP IF HIGHSCORE REACHED
                if (fitness > Pool.maxFitness)
                {
                    Pool.maxFitness = fitness;
                    Console.WriteLine("Max Fitness: " + Math.Floor(Pool.maxFitness));
                    Console.WriteLine("backup." + Pool.generation + ".");
                }

                Pool.currentSpecies = 0;        //ZERO START
                Pool.currentGenome = 0;         //ZERO START

                while (fitnessAlreadyMeasured())
                { Status = "next genome"; nextGenome(); }
                Status = "dead";

                //FEEDBACK
                int currSpecies = Pool.currentSpecies + 1;
                int currGenome = Pool.currentGenome + 1;

                //LEARN PANEL CALLBACK FUNCTION
                NetMain.RoundFinished(Score);

                //INITIALIZE RUN
                initializeRun();

                double measured = 0;
                double total = 0;
                foreach (newSpecies spec in Pool.species)
                    foreach (newGenome gen in spec.genomes)
                    {
                        total = total + 1;
                        if (gen.fitness != 0)
                            measured = measured + 1;
                        Status = "measure";
                    }

                //CONSOLE FEEDBACK
                Pool.measured = Math.Floor(measured / total * 100);
                DurationRun = StartRun - DateTime.Now;
                NetMain.setConsoleInvoke("Round finished!\tDuration: " + DurationRun.ToString(@"mm\:ss") + "\tGeneration: " + Pool.generation + "\tSpecies: " + currSpecies + "\tStale: " + Pool.species[Pool.currentSpecies].staleness + "\tGenome: " + currGenome + "\tFitness: " + fitness + "\tMeasured: " + Pool.measured + "%");

                //RESET GAME
                Reset = true;
            }
        }

        public static double Sigmoid(double x)
        {
            //SIGMOID FUNCTION
            return 2 / (1 + Math.Exp(-4.9 * x)) - 1;
        }

        private double Random()
        {
            //GET RANDOM DOUBLE [0, 1]
            return new Random().NextDouble();
        }

        private int Random(int xFrom, int xTo)
        {
            //GET RANDOM INTEGER [From, To]
            Random rand = new Random();
            return rand.Next(xFrom, xTo + 1);
        }

        private int Max(params int[] xInt)
        {
            //GET MAX FROM A INTEGER ARRAY
            int max = int.MinValue;
            foreach (int item in xInt) if (item > max) max = item;
            return max;
        }

        public void Save()
        {
            //SAVE
            string path = Mod_File.FileSaveDialog("learning_pool " + NetMain.Cam.Width + "x" + NetMain.Cam.Height + " (" + Pool.generation + ", " + Pool.species.Count + " of " + Population + ", " + Pool.measured + "%)", FILTER.TXT);

            //ABBRUCH
            if (path == null)
                return;

            //START LOADING
            UniLoad.loadingStart();

            List<object> file = new List<object>();
            file.Add(Duration.TotalMilliseconds);
            file.Add(string.Join(" ", OutputKeys));
            file.Add(Pool.generation);
            file.Add(Pool.maxFitness);
            file.Add(Pool.species.Count);

            foreach (newSpecies species in Pool.species) //SPECIES
            {
                file.Add(species.topFitness);
                file.Add(species.staleness);
                file.Add(species.genomes.Count);

                foreach (newGenome genome in species.genomes) //GENOME
                {
                    file.Add(genome.fitness);
                    file.Add(genome.maxneuron);

                    foreach (KeyValuePair<string, double> mutation in genome.mutationRates) //MUTATION
                    {
                        file.Add(mutation.Key);
                        file.Add(mutation.Value);
                    }

                    //DONE
                    file.Add("done");

                    file.Add(genome.genes.Count);
                    foreach (newGene gene in genome.genes) //GENE
                        file.Add(gene.into + " " + gene.output + " " + gene.weight + " " + gene.innovation + " " + gene.enabled);
                }
            }

            //END LOADING
            UniLoad.loadingEnd();

            //CREATE FILE
            Mod_File.CreateFile(file.ToArray(), path);
        }

        public void Load()
        {
            //LOAD
            string[] path = Mod_File.FileOpenDialog(false, FILTER.TXT);

            //ABBRUCH
            if (path == null)
                return;

            //START LOADING
            UniLoad.loadingStart();

            //READ TXT FILE
            string[] file = Mod_TXT.readTXT(path[0]);
            int x = 0;

            //GET DURATION
            Duration = TimeSpan.FromMilliseconds(Mod_Convert.StringToDouble(file[x++]));

            //GET OUTPUT KEYS
            OutputKeys = file[x++].Split(' ');

            //INITIALIZE POOL
            Initialize(OutputKeys, true);
            Pool.generation = Mod_Convert.StringToInteger(file[x++]);
            Pool.maxFitness = Mod_Convert.StringToDouble(file[x++]);

            int numSpecies = Mod_Convert.StringToInteger(file[x++]);
            for (int j = 0; j < numSpecies; j++) //SPECIES
            {
                newSpecies species = new newSpecies();
                Pool.species.Add(species);
                species.topFitness = Mod_Convert.StringToDouble(file[x++]);
                species.staleness = Mod_Convert.StringToInteger(file[x++]);
                int numGenomes = Mod_Convert.StringToInteger(file[x++]);
                for (int i = 0; i < numGenomes; i++) //GENOME
                {
                    newGenome genome = new newGenome();
                    species.genomes.Add(genome);
                    genome.fitness = Mod_Convert.StringToDouble(file[x++]);
                    genome.maxneuron = Mod_Convert.StringToInteger(file[x++]);

                    string line = file[x++];
                    while (line != "done")
                    {
                        genome.mutationRates[line] = Mod_Convert.StringToDouble(file[x++]);
                        line = file[x++];
                    }

                    int numGenes = Mod_Convert.StringToInteger(file[x++]);
                    for (int k = 0; k < numGenes; k++) //GENE
                    {
                        newGene gene = new newGene();

                        genome.genes.Add(gene);
                        string[] split = file[x++].Split(' ');

                        gene.into = Mod_Convert.StringToInteger(split[0]);
                        gene.output = Mod_Convert.StringToInteger(split[1]);
                        gene.weight = Mod_Convert.StringToDouble(split[2]);
                        gene.innovation = Mod_Convert.StringToInteger(split[3]);
                        gene.enabled = Mod_Convert.ObjectToBool(split[4]);
                    }
                }
            }
            //FITNESS ALREADY MEASURED
            while (fitnessAlreadyMeasured())
                nextGenome();

            initializeRun();

            //UPDATE LEARN PANEL
            NetMain.RoundFinished(0);

            //END LOADING
            UniLoad.loadingEnd();
        }

        private newGenome crossover(newGenome g1, newGenome g2)
        {

            //MAKE SURE G1 IS THE HIGHER FITNESS GENOME
            if (g2.fitness > g1.fitness)
            {
                newGenome tempg = g1;
                g1 = g2;
                g2 = tempg;
            }

            newGenome child = new newGenome();

            Dictionary<int, newGene> innovations2 = new Dictionary<int, newGene>();
            for (int i = 0; i < g2.genes.Count; i++)
            {
                newGene gene = g2.genes[i];
                innovations2[gene.innovation] = gene;
            }

            for (int i = 0; i < g1.genes.Count; i++)
            {
                newGene gene1 = g1.genes[i];
                newGene gene2 = innovations2[gene1.innovation];

                //COPY GENE1 OR GENE2
                if (gene2 != null && Random(1, 2) == 1 && gene2.enabled) child.genes.Add(gene2.Copy());
                else child.genes.Add(gene1.Copy());
            }

            //GET MAXIMUM
            child.maxneuron = Max(g1.maxneuron, g2.maxneuron);

            foreach (KeyValuePair<string, double> item in g1.mutationRates)
                child.mutationRates[item.Key] = item.Value;

            return child;
        }

        private int randomNeuron(List<newGene> genes, bool nonInput)
        {
            //RANDOM NEURON
            Dictionary<int, bool> neurons = new Dictionary<int, bool>();

            //LOOP INPUTS
            if (!nonInput)
                for (int i = 0; i < InputsCount; i++)
                    neurons[i] = true;

            //LOOP OUTPUTS
            for (int i = 0; i < OutputsCount; i++)
                neurons[MaxNodes + i] = true;

            //LOOP GENES
            if (genes != null)
                for (int i = 0; i < genes.Count; i++)
                {
                    if (!nonInput || genes[i].into > InputsCount)
                        neurons[genes[i].into] = true;

                    if (!nonInput || genes[i].output > InputsCount)
                        neurons[genes[i].output] = true;
                }

            int count = 0;
            foreach (KeyValuePair<int, bool> item in neurons)
                count = count + 1;

            //GET RANDOM BETWEEN 1 AND COUNT
            int n = Random(1, count);

            foreach (KeyValuePair<int, bool> item in neurons)
            {
                n = n - 1;
                if (n == 0) return item.Key;
            }

            return 0;
        }

        private bool containsLink(List<newGene> genes, newGene link)
        {
            //CONTAINS LINK
            if (genes != null)
                for (int i = 0; i < genes.Count; i++) //LOOP GENES
                {
                    newGene gene = genes[i];
                    if (gene.into == link.into && gene.output == link.output)
                        return true;
                }
            return false;
        }

        private void pointMutate(newGenome genome)
        {
            //POINT MUTATE
            double step = genome.mutationRates["step"];

            //LOOP GENES
            if (genome.genes != null)
                for (int i = 0; i < genome.genes.Count; i++)
                {
                    if (Random() < PerturbChance) genome.genes[i].weight = genome.genes[i].weight + Random() * step * 2 - step;
                    else genome.genes[i].weight = Random() * 4 - 2;
                }
        }

        private void linkMutate(newGenome genome, bool forceBias)
        {
            //LINK MUTATE
            int neuron1 = randomNeuron(genome.genes, false);
            int neuron2 = randomNeuron(genome.genes, true);

            newGene newLink = new newGene();
            if (neuron1 <= InputsCount && neuron2 <= InputsCount) //BOTH INPUT NODES
                return;

            if (neuron2 <= InputsCount) //SWAP OUTPUT AND INPUT
            {
                int temp = neuron1;
                neuron1 = neuron2;
                neuron2 = temp;
            }

            newLink.into = neuron1;
            newLink.output = neuron2;
            if (forceBias)
                newLink.into = InputsCount;

            if (containsLink(genome.genes, newLink))
                return;

            newLink.innovation = newInnovation(Pool);
            newLink.weight = Random() * 4 - 2;

            //ADD TO GENES LIST
            genome.genes.Add(newLink);
        }

        private void nodeMutate(newGenome genome)
        {
            //NODE MUTATE
            if (genome.genes.Count == 0)
                return;

            genome.maxneuron = genome.maxneuron + 1;

            //GET RANDOM GENE
            newGene gene = genome.genes[Random(0, genome.genes.Count - 1)];

            if (!gene.enabled) //IF FALSE RETURN
                return;

            gene.enabled = false;

            //ADD GENE1
            newGene gene1 = gene.Copy();
            gene1.output = genome.maxneuron;
            gene1.weight = 1.0;
            gene1.innovation = newInnovation(Pool);
            gene1.enabled = true;
            genome.genes.Add(gene1);

            //ADD GENE2
            newGene gene2 = gene.Copy();
            gene2.into = genome.maxneuron;
            gene2.innovation = newInnovation(Pool);
            gene2.enabled = true;
            genome.genes.Add(gene2);
        }

        private void enableDisableMutate(newGenome genome, bool enable)
        {
            //ENABLE DISABLE MUTATE
            List<newGene> candidates = new List<newGene>();

            //GET ENABLES GENES
            foreach (newGene item in genome.genes)
                if (item.enabled == !enable)
                    candidates.Add(item);

            if (candidates.Count == 0)
                return;

            //GET RANDOM GENE FROM CANIDATES
            newGene gene = candidates[Random(0, candidates.Count - 1)];
            gene.enabled = !gene.enabled;
        }

        public void mutate(newGenome genome)
        {
            //MUTATE
            foreach (string key in genome.mutationRates.Keys.ToList())
            {
                if (Random(1, 2) == 1) genome.mutationRates[key] = 0.95 * genome.mutationRates[key];  //KONSTANT 0.95
                else genome.mutationRates[key] = 1.05263 * genome.mutationRates[key];                 //KONSTANT 1.05263
            }

            //POINT MUTATE
            if (Random() < genome.mutationRates["connections"])
                pointMutate(genome);

            double p = genome.mutationRates["link"]; //LINK MUTATE BY LINK RATE
            while (p > 0)
            {
                if (Random() < p) linkMutate(genome, false);
                p = p - 1;
            }

            p = genome.mutationRates["bias"]; //LINK MUTATE BY BIAS RATE
            while (p > 0)
            {
                if (Random() < p) linkMutate(genome, true);
                p = p - 1;
            }

            p = genome.mutationRates["node"]; //NODE MUTATE BY NODE RATE
            while (p > 0)
            {
                if (Random() < p) nodeMutate(genome);
                p = p - 1;
            }

            p = genome.mutationRates["enable"]; //ENABLE DISABLE MUTATE BY ENABLE RATE
            while (p > 0)
            {
                if (Random() < p) enableDisableMutate(genome, true);
                p = p - 1;
            }

            p = genome.mutationRates["disable"]; //ENABLE DISABLE MUTATE BY DISABLE RATE
            while (p > 0)
            {
                if (Random() < p) enableDisableMutate(genome, false);
                p = p - 1;
            }
        }

        private double disjoint(List<newGene> genes1, List<newGene> genes2)
        {
            //DISJOINT
            Dictionary<int, bool> i1 = new Dictionary<int, bool>();

            //LOOP GENES1
            for (int i = 0; i < genes1.Count; i++)
            {
                newGene gene = genes1[i];
                i1[gene.innovation] = true;
            }

            Dictionary<int, bool> i2 = new Dictionary<int, bool>();

            //LOOP GENES2 
            for (int i = 0; i < genes2.Count; i++)
            {
                newGene gene = genes2[i];
                i2[gene.innovation] = true;
            }

            //DISJOINT GENES1
            int disjointGenes = 0;
            for (int i = 0; i < genes1.Count; i++)
            {
                newGene gene = genes1[i];
                if (i2.Keys.Contains(gene.innovation) && !i2[gene.innovation]) //i2.Keys.Contains(gene.innovation) ADD AFTER ERROR (KEY DON'T FIND)
                    disjointGenes = disjointGenes + 1;
            }

            //DISJOINT GENES2
            for (int i = 0; i < genes2.Count; i++)
            {
                newGene gene = genes2[i];
                if (i1.Keys.Contains(gene.innovation) && !i1[gene.innovation]) //i1.Keys.Contains(gene.innovation) ADD AFTER ERROR (KEY DON'T FIND)
                    disjointGenes = disjointGenes + 1;
            }

            int n = Max(genes1.Count, genes2.Count);
            return (double)disjointGenes / n;
        }

        private double weights(List<newGene> genes1, List<newGene> genes2)
        {
            //WEIGHTS
            Dictionary<int, newGene> i2 = new Dictionary<int, newGene>();
            for (int i = 0; i < genes2.Count; i++)
            {
                newGene gene = genes2[i];
                i2[gene.innovation] = gene; //GENE DICTIONARY
            }


            double sum = 0;
            double coincident = 0;
            for (int i = 0; i < genes1.Count; i++)
            {
                newGene gene = genes1[i];
                if (i2.Keys.Contains(gene.innovation))
                {
                    newGene gene2 = i2[gene.innovation];
                    sum = sum + Math.Abs(gene.weight - gene2.weight);
                    coincident = coincident + 1;
                }
            }

            return sum / coincident;
        }

        private bool sameSpecies(newGenome genome1, newGenome genome2)
        {
            //SAME SPECIES
            double dd = DeltaDisjoint * disjoint(genome1.genes, genome2.genes);
            double dw = DeltaWeights * weights(genome1.genes, genome2.genes);
            return dd + dw < DeltaThreshold;
        }

        private void rankGlobally()
        {
            List<newGenome> global = new List<newGenome>();
            for (int j = 0; j < Pool.species.Count; j++)
            {
                newSpecies species = Pool.species[j];
                for (int i = 0; i < species.genomes.Count; i++)
                    global.Add(species.genomes[i]);
            }

            //SORT BY FITNESS
            global.Sort(); //function(a, b) return (a.fitness < b.fitness) end)

            //SET GLOBAL RANK
            for (int i = 0; i < global.Count; i++)
                global[i].globalRank = i;
        }

        private void calculateAverageFitness(newSpecies species)
        {
            //CALCULATE AVERAGE FITNESS
            double total = 0;

            for (int i = 0; i < species.genomes.Count; i++)
            {
                newGenome genome = species.genomes[i];
                total = total + genome.globalRank;
            }

            //SET SPECIES FITNESS
            species.averageFitness = total / species.genomes.Count;
        }

        private double totalAverageFitness()
        {
            //TOTAL AVERAGE FITNESS
            double total = 0;
            for (int i = 0; i < Pool.species.Count; i++)
            {
                newSpecies species = Pool.species[i];
                total = total + species.averageFitness;
            }

            return total;
        }

        private void cullSpecies(bool cutToOne)
        {
            //CULL SPECIES (AUSSORTIEREN)
            int removed = 0;
            for (int i = 0; i < Pool.species.Count; i++)
            {
                newSpecies species = Pool.species[i];

                species.genomes.Sort(); //table.sort(species.genomes, function(a, b) return (a.fitness > b.fitness) end)
                species.genomes.Reverse(); //A.FITNESS > B.FITNESS

                double remaining = Math.Ceiling((double)species.genomes.Count / 2);     //HALF GENOME SIZE
                if (cutToOne) remaining = 1;                                            //SIZE ONE

                while (species.genomes.Count > remaining)
                {
                    species.genomes.Remove(species.genomes[species.genomes.Count - 1]); //DELETE LAST ITEM
                    removed++;
                }
            }

            //CONSOLE FEEDBACK
            NetMain.setConsoleInvoke("Cull species:\t" + removed + " genomes removed from " + Pool.species.Count + " species (cut to one = " + cutToOne + ")");
        }

        private newGenome breedChild(newSpecies species)
        {
            //BREED CHILD
            newGenome child;
            if (Random() < CrossoverChance)
            {
                newGenome g1 = species.genomes[Random(0, species.genomes.Count - 1)];
                newGenome g2 = species.genomes[Random(0, species.genomes.Count - 1)];
                child = crossover(g1, g2);
            }
            else
            {
                newGenome g = species.genomes[Random(0, species.genomes.Count - 1)];
                child = g.copyGenome();
            }

            //MUTATE CHILD
            mutate(child);
            return child;
        }

        private void removeStaleSpecies()
        {
            //REMOVE STALE SPECIES
            List<newSpecies> survived = new List<newSpecies>();

            for (int i = 0; i < Pool.species.Count; i++)
            {
                newSpecies species = Pool.species[i];

                species.genomes.Sort();     //(species.genomes, function(a, b) return (a.fitness > b.fitness) end);
                species.genomes.Reverse();  //A.FITNESS > B.FITNESS

                if (species.genomes[0].fitness > species.topFitness)
                {
                    species.topFitness = species.genomes[0].fitness;
                    species.staleness = 0;
                }
                else
                    species.staleness = species.staleness + 1;

                if (species.staleness < StaleSpecies || species.topFitness >= Pool.maxFitness)
                    survived.Add(species);
            }

            //TRANSFER TO POOL
            NetMain.setConsoleInvoke("Generation complete:\t" + (Pool.species.Count - survived.Count) + " species removed\t" + survived.Count + " survived");
            Pool.species = survived;
        }

        private void removeWeakSpecies()
        {
            //REMOVE WEAK SPECIES
            List<newSpecies> survived = new List<newSpecies>();

            double sum = totalAverageFitness();
            for (int i = 0; i < Pool.species.Count; i++)
            {
                newSpecies species = Pool.species[i];
                double breed = Math.Floor(species.averageFitness / sum * Population);
                if (breed >= 1) survived.Add(species);
            }

            //TRANSFER TO POOL
            NetMain.setConsoleInvoke("Weak species:\t" + (Pool.species.Count - survived.Count) + " removed\t" + survived.Count + " survived");
            Pool.species = survived;
        }

        private void addToSpecies(newGenome child)
        {
            //ADD TO SPECIES
            bool foundSpecies = false;

            //LOOP AND SEARCH POOL SPECIES
            for (int i = 0; i < Pool.species.Count; i++)
            {
                newSpecies species = Pool.species[i];
                if (!foundSpecies && sameSpecies(child, species.genomes[0]))
                {
                    species.genomes.Add(child);
                    foundSpecies = true;
                }
            }

            //SPECIES NOT FOUND
            if (!foundSpecies)
            {
                newSpecies childSpecies = new newSpecies();
                childSpecies.genomes.Add(child);
                Pool.species.Add(childSpecies);
            }
        }

        private void newGeneration()
        {
            //NEW GENERATION
            cullSpecies(false); //CULL THE BOTTOM HALF OF EACH SPECIES
            rankGlobally();
            removeStaleSpecies();
            rankGlobally();
            for (int i = 0; i < Pool.species.Count; i++)
            {
                newSpecies species = Pool.species[i];
                calculateAverageFitness(species);
            }

            //REMOVE WEAK SPECIES
            removeWeakSpecies();

            double sum = totalAverageFitness();

            List<newGenome> children = new List<newGenome>();
            for (int i = 0; i < Pool.species.Count; i++)
            {
                newSpecies species = Pool.species[i];

                double breed = Math.Floor(species.averageFitness / sum * Population) - 1;
                for (double d = 0; d < breed; d++) //DOUBLE LOOP
                    children.Add(breedChild(species));
            }

            cullSpecies(true); //CULL ALL BUT THE TOP MEMBER OF EACH SPECIES
            while (children.Count + Pool.species.Count < Population)
            {
                newSpecies species = Pool.species[Random(0, Pool.species.Count - 1)];
                children.Add(breedChild(species));
            }

            for (int i = 0; i < children.Count; i++)
            {
                newGenome child = children[i];
                addToSpecies(child);
            }

            //RAISE POOL GENERATION
            Pool.generation = Pool.generation + 1;

            //GENERATE BACKUP FILE
            //writeFile("backup."..pool.generation.. "."..forms.gettext(saveLoadFile))
        }

        private void initializePool(bool xLoad = false)
        {
            //INITIALIZE POOL
            Pool = new newPool();

            //ABBRUCH
            if (xLoad)
                return;

            for (int i = 0; i < Population; i++)
            {
                newGenome basic = newGenome.basicGenome(this);
                addToSpecies(basic);
            }

            //INITIALIZE RUN
            initializeRun();
        }

        private void clearJoypad()
        {
            //CLEAR JOYPAD (NO NEED FOR C#)
            for (int i = 0; i < OutputKeys.Length; i++)
                Controller[OutputKeys[i]] = false;
        }

        private void initializeRun()
        {
            //INITIALIZE RUN
            Score = 0;
            clearJoypad();

            newSpecies species = Pool.species[Pool.currentSpecies];
            newGenome genome = species.genomes[Pool.currentGenome];
            generateNetwork network = new generateNetwork(genome);
            evaluateCurrent();
        }

        private void evaluateCurrent()
        {
            //ABBRUCH
            if (!NetMain.Cam.Visible)
                return;

            //EVALUATE CURRENT 
            newSpecies species = Pool.species[Pool.currentSpecies];
            newGenome genome = species.genomes[Pool.currentGenome];

            List<double> inputs = Mod_Convert.ArrayToList<double>(NetMain.Cam.getDoubleArray());    //READ INPUTS FROM CAM          
            Controller = genome.network.evaluateNetwork(inputs);                                    //GET OUTPUTS        
        }

        private void nextGenome()
        {
            //NEXT GENOME
            Pool.currentGenome = Pool.currentGenome + 1;
            if (Pool.currentGenome >= Pool.species[Pool.currentSpecies].genomes.Count)
            {
                Pool.currentGenome = 0;             //ZERO START
                Pool.currentSpecies = Pool.currentSpecies + 1;
                if (Pool.currentSpecies >= Pool.species.Count)
                {
                    newGeneration();
                    Pool.currentSpecies = 0;        //ZERO START
                }
            }
        }

        private bool fitnessAlreadyMeasured()
        {
            //FITNESS ALREADY MEASURED
            newSpecies species = Pool.species[Pool.currentSpecies];
            newGenome genome = species.genomes[Pool.currentGenome];
            return genome.fitness != 0;
        }

        private void displayGenome(newGenome genome)
        {
            //DISPLAY GENOME
            generateNetwork network = genome.network;
            List<int[]> cells = new List<int[]>();
            int[] cell;
            int i = 1;

            for (int dy = -BoxSize.Height; dy < BoxSize.Height; dy++)
            {
                for (int dx = -BoxSize.Width; dx < BoxSize.Width; dx++)
                {
                    cell = new int[] { 50 + 5 * dx, 70 + 5 * dy, (int)network.neurons[i].value };
                    cells.Add(cell);
                    i = i + 1;
                }

                int[] biasCell = new int[] { 80, 110, (int)network.neurons[InputsCount].value };
                cells[InputsCount] = biasCell;
            }
        }

        public int newInnovation(newPool xPool)
        {
            //NEW INNOVATION
            xPool.innovation = xPool.innovation + 1;
            return xPool.innovation;
        }

        //SECTION OF NETWORK CLASSES---------------------------------------------------------------------
        public class newPool
        {
            //NEW POOL CLASS
            public List<newSpecies> species = new List<newSpecies>();
            public int generation = 0;
            public int innovation;
            public int currentSpecies = 0;          //ZERO START
            public int currentGenome = 0;           //ZERO START
            public double maxFitness = 0;
            public double measured = 0;

            public newPool()
            {
                innovation = OutputsCount;
            }
        }

        public class newSpecies
        {
            //NEW SPECIES CLASS
            public double topFitness = 0;
            public int staleness = 0;
            public List<newGenome> genomes = new List<newGenome>();
            public double averageFitness = 0;
        }

        public class newGenome : IComparable<newGenome>
        {
            //NEW GENOM CLASS
            public List<newGene> genes = new List<newGene>();
            public double fitness = 0;
            public int adjustedFitness = 0;
            public generateNetwork network;
            public int maxneuron = 0;
            public int globalRank = 0;
            public Dictionary<string, double> mutationRates = new Dictionary<string, double>();

            public newGenome()
            {
                mutationRates.Add("connections", MutateConnectionsChance);
                mutationRates.Add("link", LinkMutationChance);
                mutationRates.Add("bias", BiasMutationChance);
                mutationRates.Add("node", NodeMutationChance);
                mutationRates.Add("enable", EnableMutationChance);
                mutationRates.Add("disable", DisableMutationChance);
                mutationRates.Add("step", StepSize);
            }

            public int CompareTo(newGenome other)
            {
                //COMPARE TO
                return fitness.CompareTo(other.fitness);
            }

            public newGenome copyGenome()
            {
                //COPY GENOME
                newGenome genome = new newGenome();

                for (int i = 0; i < genome.genes.Count; i++)
                    genome.genes.Add(genome.genes[i].Copy());

                genome.maxneuron = maxneuron;
                genome.mutationRates["connections"] = mutationRates["connections"];
                genome.mutationRates["link"] = mutationRates["link"];
                genome.mutationRates["bias"] = mutationRates["bias"];
                genome.mutationRates["node"] = mutationRates["node"];
                genome.mutationRates["enable"] = mutationRates["enable"];
                genome.mutationRates["disable"] = mutationRates["disable"];
                return genome;
            }

            public static newGenome basicGenome(NetMario xMario)
            {
                //BASIC GENOME
                newGenome genome = new newGenome();

                genome.maxneuron = InputsCount;
                xMario.mutate(genome);

                return genome;
            }
        }

        public class newGene : IComparable<newGene>
        {
            //NEW GENE CLASS
            public int into = 0;
            public int output = 0;
            public double weight = 0.0;
            public bool enabled = true;
            public int innovation = 0;

            public int CompareTo(newGene other)
            {
                //COMPARE TO
                return output.CompareTo(other.output);
            }

            public newGene Copy()
            {
                //COPY GENE
                newGene gene = new newGene();
                gene.into = into;
                gene.output = output;
                gene.weight = weight;
                gene.enabled = enabled;
                gene.innovation = innovation;
                return gene;
            }
        }

        public class newNeuron
        {
            //NEW NEURON CLASS
            public List<newGene> incoming = new List<newGene>();
            public double value = 0.0;
        }

        public class generateNetwork
        {
            //GENERATE NETWORK CLASS
            public Dictionary<int, newNeuron> neurons = new Dictionary<int, newNeuron>();

            public generateNetwork(newGenome genome)
            {
                //INPUTS
                for (int i = 0; i < InputsCount; i++)
                    neurons[i] = new newNeuron();

                //OUTPUTS
                for (int i = 0; i < OutputsCount; i++)
                    neurons[MaxNodes + i] = new newNeuron();

                genome.genes.Sort(); //table.sort(genome.genes, function(a, b) return (a.output < b.output) end)

                for (int i = 0; i < genome.genes.Count; i++)
                {
                    newGene gene = genome.genes[i];
                    if (gene.enabled)
                    {
                        if (!neurons.Keys.Contains(gene.output))
                            neurons[gene.output] = new newNeuron();

                        newNeuron neuron = neurons[gene.output];
                        neuron.incoming.Add(gene);
                        if (!neurons.Keys.Contains(gene.into))
                            neurons[gene.into] = new newNeuron();
                    }
                    genome.network = this;
                }
            }

            public Dictionary<string, bool> evaluateNetwork(List<double> inputs)
            {
                //GET OUTPUT AS BOOLEAN
                Inputs = inputs.ToArray();      //SET INPUTS
                inputs.Add(1);
                if (inputs.Count != InputsCount)
                { Console.WriteLine("Incorrect number of neural network inputs."); return null; }

                for (int i = 0; i < InputsCount; i++)
                    neurons[i].value = inputs[i];

                foreach (int neuron in neurons.Keys.ToList()) //LOOP NEVER INPUTS
                {
                    double sum = 0;
                    for (int i = 0; i < neurons[neuron].incoming.Count; i++)
                    {
                        newGene incoming = neurons[neuron].incoming[i];
                        newNeuron other = neurons[incoming.into];
                        sum = sum + incoming.weight * other.value;                              //WEIGHT CALCULATION
                    }
                    if (neurons[neuron].incoming.Count > 0)
                        neurons[neuron].value = Sigmoid(sum);                                   //SIGMOID FUNCTION
                }

                Dictionary<string, bool> outputs = new Dictionary<string, bool>();
                for (int i = 0; i < OutputsCount; i++)
                {
                    string button = OutputKeys[i];                                              //GET BUTTON NAMES
                    if (neurons[MaxNodes + i].value > 0) outputs.Add(button, true);             //OUTPUT IS TRUE
                    else outputs.Add(button, false);                                            //OUTPUT IS FALSE

                    Outputs[i] = neurons[MaxNodes + i].value;                                   //SET OUTPUTS
                }

                //REFRESH IO PANEL
                NetMain.IO.setIO(Inputs, Outputs);
                NetMain.IO.SecureRefesh();

                return outputs;
            }
        }
    }
}