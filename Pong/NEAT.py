import sys
import math
import random
import datetime

#FILE DIALOG
import easygui

#NUMPY ARRAYS
import numpy as np

class AI:
    #PRE-INITIALISED        
    def __init__(self, xName, xPopulation):
        #INITIALIZE
        self.Name = xName
        self.Population = xPopulation
        self.DeltaDisjoint = 2.0
        self.DeltaWeights = 0.4
        self.DeltaThreshold = 1.0

        self.StaleSpecies = 15                  #VERJÄHRUNG EINER SPECIES

        self.MutateConnectionsChance = 0.25
        self.PerturbChance = 0.90               #CONSTANT IN POINT MUTATION
        self.CrossoverChance = 0.75
        self.LinkMutationChance = 2.0
        self.NodeMutationChance = 0.50
        self.BiasMutationChance = 0.40
        self.StepSize = 0.1                     #STEP SIZE IN POINT MUTATION
        self.DisableMutationChance = 0.4
        self.EnableMutationChance = 0.2

        self.MaxNodes = 1000000

        self.Learning = False    #LEARNING
        self.Start = 0           #START
        self.End = 0             #END
        self.Duration = None     #DURATION
        self.Reset = False       #RESET
        self.StartRun = 0        #START

        self.Status = ''         #STATUS
        self.Inputs = []         #INPUTS
        self.InputsCount = 0     #INPUTSCOUNT
        self.OutputsCount = 0    #OUTPUTSCOUNT
        self.OutputKeys = []     #OUTPUTKEYS
        self.Controller = {}     #CONTROLLER
        self.Score = 0           #SCORE

        self.Pool = None         #POOL


    def Startup(self, xOutputKeys):
        #START LEARNING
        if not (self.Learning):
            self.Initialize(xOutputKeys, False)
            self.Start = datetime.datetime.now() 
            self.Learning = True
            print('Machine learning starts [' + str(self) + ']...')

    
    def Stop(self):
        #STOP LEARNING
        self.Learning = False
        self.End = datetime.datetime.now()
        self.Duration = self.End - self.Start
        print('Machine learning ends: ' + str(self.Duration) + ' [' + str(self) + ']...')


    def Initialize(self, xOutputKeys, xLoad):
        #INITIALIZE INPUTS AND OUTPUTS           
        self.InputsCount = len(self.Inputs) + 1
        self.OutputsCount = len(xOutputKeys)
        self.OutputKeys = xOutputKeys
        self.Outputs = np.zeros(self.OutputsCount) #DEFINE OUTPUT VECTOR SIZE
        
        #INITIALIZE POOL
        self.initializePool(xLoad)


    def Run(self):
        #LEARNING RUN
        if not (self.Learning):
            self.Status = 'stopped'
            return

        self.Status = 'alive'

        #CONVERT INPUTS TO OUTPUTS
        self.evaluateCurrent()

        return self.Controller

    def Evolution(self):
        #EVOLUTION NETWORK
        self.Status = 'reset'

        #GET SCORE
        score = self.Score #SCORE = TIME -> T-REX RUNNING   

        fitness = score
        if (score > 5000): #INPUT BONUS SCORE
            fitness = fitness + 500

        if (fitness == 0):
            fitness = -1

        #GET CURRENT SPECIES & GENOME
        species = self.Pool.species[self.Pool.currentSpecies]
        genome = species.genomes[self.Pool.currentGenome]
        genome.fitness = fitness

        #MAKE BACKUP IF HIGHSCORE REACHED
        if (fitness > self.Pool.maxFitness):
            self.Pool.maxFitness = fitness
            print('Max Fitness: ' + str(math.floor(self.Pool.maxFitness)))

        self.Pool.currentSpecies = 0        #ZERO START
        self.Pool.currentGenome = 0         #ZERO START

        while (self.fitnessAlreadyMeasured()):
            self.Status = 'next genome'; self.nextGenome()
        self.Status = 'dead'

        #INITIALIZE RUN
        DurationRun = datetime.datetime.now() - self.StartRun
        self.initializeRun()

        measured = 0.0
        total = 0.0
        for spec in self.Pool.species:

            for gen in spec.genomes:
                total = total + 1

                if (gen.fitness != 0):
                    measured = measured + 1
                self.Status = 'measure'

        #FEEDBACK
        self.Pool.measured = math.floor(measured / total * 100)
        print('Round finished!\tDuration: ' + str(DurationRun)[:str(DurationRun).find('.')] + '\tGeneration: ' + str(self.Pool.generation) + '\tSpecies: ' + str(self.Pool.currentSpecies + 1) + '\tStale: ' + str(self.Pool.species[self.Pool.currentSpecies].staleness) + '\tGenome: ' + str(self.Pool.currentGenome + 1) + '\tFitness: ' + str(fitness) + '\tMeasured: ' + str(self.Pool.measured) + '%')
        self.Alive = True


    def Sigmoid(self, xFloat):
        #SIGMOID FUNCTION
        return 2 / (1 + math.exp(-4.9 * xFloat)) - 1


    def RandomFloat(self):
        #GET RANDOM DOUBLE [0, 1]
        return random.uniform(0, 1)


    def RandomInteger(self, xFrom, xTo):
        #GET RANDOM INTEGER [From, To]
        return random.randint(xFrom, xTo)


    def Max(self, xIntegerArray):
        #GET MAX FROM A INTEGER ARRAY
        maximum = -sys.maxsize -1
        for item in xIntegerArray: 
            if (item > maximum): maximum = item
        return maximum


    def Save(self):
        #SAVE
        sep = ' '
        save = []
        print('Duration: ' + str(self.Duration))
        save.append(self.Duration.total_seconds() * 1000)
        save.append(sep.join(self.OutputKeys))
        save.append(self.Pool.generation)
        save.append(self.Pool.maxFitness)
        save.append(len(self.Pool.species))

        for species in self.Pool.species: #SPECIES
            save.append(species.topFitness)
            save.append(species.staleness)
            save.append(len(species.genomes))

            for genome in species.genomes: #GENOME
                save.append(genome.fitness)
                save.append(genome.maxneuron)

                for key, value in genome.mutationRates.items(): #MUTATION
                    save.append(key)
                    save.append(value)

                #DONE
                save.append('done')

                save.append(len(genome.genes))
                for gene in genome.genes: #GENE
                    save.append(str(gene.into) + ' ' + str(gene.output) + ' ' + str(gene.weight) + ' ' + str(gene.innovation) + ' ' + str(gene.enabled))

        #CREATE FILE
        f = open('learning_pool ' + str(self.InputsCount) + ' (' + str(self.Pool.generation) + ', ' + str(len(self.Pool.species)) + ' of ' + str(self.Population) + ', ' + str(self.Pool.measured) + '%)','w+')
        for line in save:
            f.write(str(line) + '\r\n')
        f.close()  


    def Load(self):
        #LOAD
        path = easygui.fileopenbox()

        #BREAK UP
        if (path is None):
            return

        #READ TXT FILE
        load = open(path, 'r')
        x = 0

        #GET DURATION
        self.Duration = datetime.timedelta(milliseconds=float(load[x])); x += 1

        #GET OUTPUT KEYS
        OutputKeys = load[x].split(' '); x += 1

        #INITIALIZE POOL
        self.Initialize(OutputKeys, True)
        self.Pool.generation = int(load[x]); x += 1
        self.Pool.maxFitness = float(load[x]); x += 1

        numSpecies = int(load[x]); x += 1

        for _ in range(numSpecies): #SPECIES
            species = newSpecies()
            self.Pool.species.append(species)
            species.topFitness = float(load[x]); x += 1
            species.staleness = int(load[x]); x += 1
            numGenomes = int(load[x]); x += 1

            for _ in range(numGenomes): #GENOME
                genome = newGenome(self)
                species.genomes.append(genome)
                genome.fitness = float(load[x]); x += 1
                genome.maxneuron = int(load[x]); x += 1

                line = load[x]; x += 1
                while (line != 'done'):            
                    genome.mutationRates[line] = float(load[x]); x += 1
                    line = load[x]; x += 1

                numGenes =int(load[x]); x += 1
                
                for _ in range(numGenes): #GENE
                    gene = newGene()

                    genome.genes.append(gene)
                    split = load[x].split(' '); x += 1

                    gene.into = int(split[0])
                    gene.output = int(split[1])
                    gene.weight = float(split[2])
                    gene.innovation = int(split[3])
                    gene.enabled = split[4] == 'True'
                
            
        #FITNESS ALREADY MEASURED
        while (self.fitnessAlreadyMeasured()):
            self.nextGenome()

        self.initializeRun()


    def crossover(self, g1, g2): #newGenome
        #MAKE SURE G1 IS THE HIGHER FITNESS GENOME
        if (len(g1.genes) > len(g2.genes)): #g2.fitness > g1.fitness or 
            tempg = g1
            g1 = g2
            g2 = tempg
        
        #CHECK LIST1 IS IN LIST2
        list1 = []
        for g in g1.genes: list1.append(g.innovation)
        list2 = []
        for g in g2.genes: list2.append(g.innovation)
        same = all(elem in list2 for elem in list1)

        child = newGenome(self) #CREATE NEW CHILD GENOME

        innovations2 = {} #int, newGene
        for i in range(len(g2.genes)):
            gene = g2.genes[i]
            innovations2[gene.innovation] = gene
        
        for i in range(len(g1.genes)):
            gene1 = g1.genes[i]
            if(same): #ADDED FOR BUG-FIX
                gene2 = innovations2[gene1.innovation] #ERROR GENE1.INNOVATION IST NICHT IN DEM DICT INNOVATIONS2 VORHANDEN (KEY ERROR)

                #COPY GENE1 OR GENE2
                if (gene2 is not None and self.RandomInteger(1, 2) == 1 and gene2.enabled): child.genes.append(gene2.Copy())
                else: child.genes.append(gene1.Copy())

        #GET MAXIMUM
        child.maxneuron = self.Max((g1.maxneuron, g2.maxneuron))       

        for key, value in g1.mutationRates.items():
            child.mutationRates[key] = value

        return child


    def randomNeuron(self, genes, nonInput):
        #RANDOM NEURON
        neurons = {} #INT, BOOL

        #LOOP INPUTS
        if not (nonInput):
            for i in range(self.InputsCount):
                neurons[i] = True

        #LOOP OUTPUTS
        for i in range(self.OutputsCount):
            neurons[self.MaxNodes + i] = True

        #LOOP GENES
        if (genes is not None):
            for i in range(len(genes)):

                if (not nonInput or genes[i].into > self.InputsCount):
                    neurons[genes[i].into] = True

                if (not nonInput or genes[i].output > self.InputsCount):
                    neurons[genes[i].output] = True

        count = 0
        for key in neurons.items():
            count = count + 1

        #GET RANDOM BETWEEN 1 AND COUNT
        n = self.RandomInteger(1, count)

        for key, _ in neurons.items():
            n = n - 1
            if (n == 0): return key

        return 0


    def containsLink(self, genes, link): #List<newGene>, newGene
        #CONTAINS LINK
        if (genes is not None):
            for i in range(len(genes)): #LOOP GENES
                gene = genes[i]
                if (gene.into == link.into and gene.output == link.output):
                    return True
        return False


    def pointMutate(self, genome): #newGenome
        #POINT MUTATE
        step = genome.mutationRates['step']

        #LOOP GENES
        if (genome.genes is not None):
            for i in range(len(genome.genes)):
                if (self.RandomFloat() < self.PerturbChance): genome.genes[i].weight = genome.genes[i].weight + self.RandomFloat() * step * 2 - step
                else: genome.genes[i].weight = self.RandomFloat() * 4 - 2


    def linkMutate(self, genome, forceBias): #newGenome, bool
        #LINK MUTATE
        neuron1 = self.randomNeuron(genome.genes, False)
        neuron2 = self.randomNeuron(genome.genes, True)
        
        newLink = newGene()
        if (neuron1 <= self.InputsCount and neuron2 <= self.InputsCount): #BOTH INPUT NODES
            return
            
        if (neuron2 <= self.InputsCount): #SWAP OUTPUT AND INPUT
            temp = neuron1
            neuron1 = neuron2
            neuron2 = temp

        newLink.into = neuron1
        newLink.output = neuron2

        if (forceBias):
            newLink.into = self.InputsCount

        if (self.containsLink(genome.genes, newLink)):
            return
            
        newLink.innovation = self.newInnovation(self.Pool)
        newLink.weight = self.RandomFloat() * 4 - 2

        #ADD TO GENES LIST        
        genome.genes.append(newLink)


    def nodeMutate(self, genome): #newGenome
        #NODE MUTATE
        if (len(genome.genes) == 0):
            return

        genome.maxneuron = genome.maxneuron + 1

        #GET RANDOM GENE
        rand = self.RandomInteger(0, len(genome.genes) - 1)
        gene = genome.genes[rand]

        if not (gene.enabled): #IF FALSE RETURN
            return

        gene.enabled = False

        #ADD GENE1
        gene1 = gene.Copy()
        gene1.output = genome.maxneuron
        gene1.weight = 1.0
        gene1.innovation = self.newInnovation(self.Pool)
        gene1.enabled = True
        genome.genes.append(gene1)

        #ADD GENE2
        gene2 = gene.Copy()
        gene2.into = genome.maxneuron
        gene2.innovation = self.newInnovation(self.Pool)
        gene2.enabled = True
        genome.genes.append(gene2)


    def enableDisableMutate(self, genome, enable): #newGenome, bool
        #ENABLE DISABLE MUTATE
        candidates = []

        #GET ENABLES GENES
        for item in genome.genes:
            if (item.enabled != enable):
                candidates.append(item)

        if (len(candidates) == 0):
            return

        #GET RANDOM GENE FROM CANIDATES
        gene = candidates[self.RandomInteger(0, len(candidates) - 1)]
        gene.enabled = not gene.enabled


    def mutate(self, genome): #newGenome
        
        #MUTATE
        for key, _ in genome.mutationRates.items():
            if (self.RandomInteger(1, 2) == 1): genome.mutationRates[key] = 0.95 * genome.mutationRates[key]  #KONSTANT 0.95
            else: genome.mutationRates[key] = 1.05263 * genome.mutationRates[key]                 #KONSTANT 1.05263

        #POINT MUTATE
        if (self.RandomFloat() < genome.mutationRates['connections']):
            self.pointMutate(genome)

        p = genome.mutationRates['link'] #LINK MUTATE BY LINK RATE
        while (p > 0):
            ran = self.RandomFloat()
            if (ran < p): self.linkMutate(genome, False)
            p = p - 1

        p = genome.mutationRates['bias'] #LINK MUTATE BY BIAS RATE
        while (p > 0):
            if (self.RandomFloat() < p): self.linkMutate(genome, True)
            p = p - 1

        p = genome.mutationRates['node'] #NODE MUTATE BY NODE RATE
        while (p > 0):
            if (self.RandomFloat() < p): self.nodeMutate(genome)
            p = p - 1

        p = genome.mutationRates['enable'] #ENABLE DISABLE MUTATE BY ENABLE RATE
        while (p > 0):
            if (self.RandomFloat() < p): self.enableDisableMutate(genome, True)
            p = p - 1

        p = genome.mutationRates['disable'] #ENABLE DISABLE MUTATE BY DISABLE RATE
        while (p > 0):
            if (self.RandomFloat() < p): self.enableDisableMutate(genome, False)
            p = p - 1


    def disjoint(self, genes1, genes2): #List<newGene>, List<newGene>
        #DISJOINT
        i1 = {} #int, bool

        #LOOP GENES1
        for i in range(len(genes1)):
            gene = genes1[i]
            i1[gene.innovation] = True

        i2 = {} #int, bool

        #LOOP GENES2 
        for i in range(len(genes2)):
            gene = genes2[i]
            i2[gene.innovation] = True

        #DISJOINT GENES1
        disjointGenes = 0
        for i in range(len(genes1)):
            gene = genes1[i]
            #if (gene.innovation in i2 and not i2[gene.innovation]):
            if not (gene.innovation in i2): #i2.Keys.Contains(gene.innovation) ADD AFTER ERROR (KEY DON'T FIND)
                disjointGenes = disjointGenes + 1

        #DISJOINT GENES2
        for i in range(len(genes2)):
            gene = genes2[i]
            #if (gene.innovation in i1 and not i1[gene.innovation]):
            if not (gene.innovation in i1): #i1.Keys.Contains(gene.innovation) ADD AFTER ERROR (KEY DON'T FIND)
                disjointGenes = disjointGenes + 1

        n = self.Max((len(genes1), len(genes2)))
        if(n == 0): return sys.maxsize #DIVISION ZERO SHOULD RETURN A VERY LARGE NUMBER
        return float(disjointGenes) / float(n)


    def weights(self, genes1, genes2): #List<newGene>, List<newGene>
        #WEIGHTS
        i2 = {} #int, newGene
        for i in range(len(genes2)):
            gene = genes2[i]
            i2[gene.innovation] = gene #GENE DICTIONARY

        summe = 0.0
        coincident = 0.0
        for i in range(len(genes1)):
            gene = genes1[i]

            if (gene.innovation in i2):
                gene2 = i2[gene.innovation]
                summe = summe + abs(gene.weight - gene2.weight)
                coincident = coincident + 1
        
        if(coincident == 0): return sys.maxsize #DIVISION ZERO SHOULD RETURN A VERY LARGE NUMBER
        return summe / coincident


    def sameSpecies(self, genome1, genome2): #newGenome, newGenome
        #SAME SPECIES
        dd = self.DeltaDisjoint * self.disjoint(genome1.genes, genome2.genes)
        dw = self.DeltaWeights * self.weights(genome1.genes, genome2.genes)
        return dd + dw < self.DeltaThreshold


    def rankGlobally(self):
        #RANK GLOBALLY
        globalList = [] #newGenome
        for j in range(len(self.Pool.species)):
            species = self.Pool.species[j]

            for i in range(len(species.genomes)):
                globalList.append(species.genomes[i])

        #SORT BY FITNESS
        globalList = sorted(globalList) #function(a, b) return (a.fitness < b.fitness) end)

        #SET GLOBAL RANK
        for i in range(len(globalList)):
            globalList[i].globalRank = i


    def calculateAverageFitness(self, species): #newSpecies
        #CALCULATE AVERAGE FITNESS
        total = 0.0

        for i in range(len(species.genomes)):
            genome = species.genomes[i]
            total = total + genome.globalRank

        #SET SPECIES FITNESS
        species.averageFitness = total / float(len(species.genomes))


    def totalAverageFitness(self):
        #TOTAL AVERAGE FITNESS
        total = 0.0

        for i in range(len(self.Pool.species)):
            species = self.Pool.species[i]
            total = total + species.averageFitness

        return total


    def cullSpecies(self, cutToOne): #bool
        #CULL SPECIES (AUSSORTIEREN)
        removed = 0
        for i in range(len(self.Pool.species)):
            species = self.Pool.species[i]
                        
            species.genomes = sorted(species.genomes)           #table.sort(species.genomes, function(a, b) return (a.fitness > b.fitness) end)
            species.genomes = list(reversed(species.genomes))   #A.FITNESS > B.FITNESS
            remaining = float(math.ceil(float(len(species.genomes)) / 2.0))         #HALF GENOME SIZE
            
            if (cutToOne): remaining = 1                                            #SIZE ONE
            
            while (len(species.genomes) > remaining):
                species.genomes.remove(species.genomes[len(species.genomes) - 1])   #DELETE LAST ITEM
                removed += 1

        #CONSOLE FEEDBACK
        print('Cull species:\t' + str(removed) + ' genomes removed from ' + str(len(self.Pool.species)) + ' species (cut to one = ' + str(cutToOne) + ')')


    def breedChild(self, species): #newSpecies
        #BREED CHILD
        if (self.RandomFloat() < self.CrossoverChance):
            #Hier könnte der Key-Error behoben werden, in dem man das Genom[0] verwendet
            g1 = species.genomes[self.RandomInteger(0, len(species.genomes) - 1)]
            g2 = species.genomes[self.RandomInteger(0, len(species.genomes) - 1)]
            child = self.crossover(g1, g2)
        else:
            g = species.genomes[self.RandomInteger(0, len(species.genomes) - 1)]
            child = g.copyGenome(self)

        #MUTATE CHILD
        self.mutate(child)
        return child


    def removeStaleSpecies(self):
        #REMOVE STALE SPECIES
        survived = [] #newSpecies

        for i in range(len(self.Pool.species)):
            species = self.Pool.species[i]

            species.genomes = sorted(species.genomes)           #(species.genomes, function(a, b) return (a.fitness > b.fitness) end)
            species.genomes = list(reversed(species.genomes))   #A.FITNESS > B.FITNESS        

            if (species.genomes[0].fitness > species.topFitness):
                species.topFitness = species.genomes[0].fitness
                species.staleness = 0
            else:
                species.staleness = species.staleness + 1

            if (species.staleness < self.StaleSpecies or species.topFitness >= self.Pool.maxFitness):
                survived.append(species)

        #TRANSFER TO POOL
        print('Generation complete:\t' + str(len(self.Pool.species) - len(survived)) + ' species removed\t' + str(len(survived)) + ' survived')
        self.Pool.species = survived


    def removeWeakSpecies(self):
        #REMOVE WEAK SPECIES
        survived = [] #newSpecies

        summe = self.totalAverageFitness()
        for i in range(len(self.Pool.species)):
            species = self.Pool.species[i]
            breed = math.floor(species.averageFitness / summe * self.Population)

            if (breed >= 1): survived.append(species)

        #TRANSFER TO POOL
        print('Weak species:\t' + str(len(self.Pool.species) - len(survived)) + ' removed\t' + str(len(survived)) + ' survived')
        self.Pool.species = survived


    def addToSpecies(self, child): #newGenome
        #ADD TO SPECIES
        foundSpecies = False

        #LOOP AND SEARCH POOL SPECIES
        for i in range(len(self.Pool.species)):
            species = self.Pool.species[i]

            #species.genomes[0] verursacht ein Fehler da quasi immer nur der Stamm auf Ähnlichkeit mit dem
            #potenziellen Child geprüft wird, dass ermöglicht das voneinander verschiedene Genome in einer Species landen
            if (not foundSpecies and self.sameSpecies(child, species.genomes[0])): #ORIGINAL species.genomes[0])
                species.genomes.append(child)
                foundSpecies = True

        #SPECIES NOT FOUND
        if not (foundSpecies):
            childSpecies = newSpecies()
            childSpecies.genomes.append(child)
            self.Pool.species.append(childSpecies)


    def newGeneration(self):
        #NEW GENERATION
        self.cullSpecies(False) #CULL THE BOTTOM HALF OF EACH SPECIES
        self.rankGlobally()
        self.removeStaleSpecies()
        self.rankGlobally()

        #CALCULATE AVERAGE FITNESS
        for i in range(len(self.Pool.species)):
            species = self.Pool.species[i]
            self.calculateAverageFitness(species)

        #REMOVE WEAK SPECIES
        self.removeWeakSpecies()

        summe = self.totalAverageFitness()

        children = [] #newGenome
        for i in range(len(self.Pool.species)):
            species = self.Pool.species[i]

            d = 0.0
            breed = math.floor(species.averageFitness / summe * self.Population) - 1

            #DOUBLE LOOP BREED
            while d < breed:
                children.append(self.breedChild(species))
                d += 1.0

        self.cullSpecies(True) #CULL ALL BUT THE TOP MEMBER OF EACH SPECIES
        while (len(children) + len(self.Pool.species) < self.Population):
            species = self.Pool.species[self.RandomInteger(0, len(self.Pool.species) - 1)]
            children.append(self.breedChild(species))

        #ADD GENOMES TO SPECIES
        for i in range(len(children)):
            child = children[i]
            self.addToSpecies(child)

        #RAISE POOL GENERATION
        self.Pool.generation = self.Pool.generation + 1

        #GENERATE BACKUP FILE
        #writeFile('backup.'..pool.generation.. '.'..forms.gettext(saveLoadFile))


    def initializePool(self, xLoad): #bool
        #INITIALIZE POOL
        self.Pool = newPool()

        #BREAK UP
        if (xLoad):
            return

        for _ in range(self.Population):
            basic = newGenome.basicGenome(newGenome, self)
            self.addToSpecies(basic)

        #INITIALIZE RUN
        self.initializeRun()


    def clearJoypad(self):
        #CLEAR JOYPAD (NO NEED FOR C#)
        for i in range(len(self.OutputKeys)):
            self.Controller[self.OutputKeys[i]] = False


    def initializeRun(self):
        #INITIALIZE RUN
        self.Score = 0
        self.clearJoypad()
        
        species = self.Pool.species[self.Pool.currentSpecies]
        genome = species.genomes[self.Pool.currentGenome]
        generateNetwork(self, genome)
        #self.evaluateCurrent() #OUTLINED BECAUSE INPUTS UPDATED EXTERNAL [].append(1) HAPPEN 2 TIMES

        self.StartRun = datetime.datetime.now() #RUN START


    def evaluateCurrent(self): #double[]
        #EVALUATE CURRENT 
        species = self.Pool.species[self.Pool.currentSpecies]
        genome = species.genomes[self.Pool.currentGenome]
        self.Controller = genome.network.evaluateNetwork(self)      #GET OUTPUTS        


    def nextGenome(self):
        #NEXT GENOME
        self.Pool.currentGenome = self.Pool.currentGenome + 1
        if (self.Pool.currentGenome >= len(self.Pool.species[self.Pool.currentSpecies].genomes)):
            self.Pool.currentGenome = 0             #ZERO START
            self.Pool.currentSpecies = self.Pool.currentSpecies + 1
            
            if (self.Pool.currentSpecies >= len(self.Pool.species)):
                self.newGeneration()
                self.Pool.currentSpecies = 0        #ZERO START


    def fitnessAlreadyMeasured(self):
        #FITNESS ALREADY MEASURED
        species = self.Pool.species[self.Pool.currentSpecies]
        genome = species.genomes[self.Pool.currentGenome]
        return genome.fitness != 0


    def displayGenome(self, genome): #newGenome
        #DISPLAY GENOME
        network = genome.network
        cells = [] #int
        i = 1
        BoxSize = [600, 400]

        for dy in range(-BoxSize[1], BoxSize[1]):
            for dx in range(-BoxSize[0], BoxSize[0]):
                cell = (50 + 5 * dx, 70 + 5 * dy, int(network.neurons[i].value))
                cells.append(cell)
                i = i + 1

            biasCell = (80, 110, int(network.neurons[self.InputsCount].value))
            cells[self.InputsCount] = biasCell


    def newInnovation(self, xPool):
        #NEW INNOVATION
        xPool.innovation += 1
        return xPool.innovation


#SECTION OF NETWORK CLASSES---------------------------------------------------------------------
class newPool:
    #NEW POOL CLASS
    def __init__(self):
        #INITIALIZE
        self.species = [] #newSpecies
        self.generation = 0
        self.innovation = 0
        self.currentSpecies = 0          #ZERO START
        self.currentGenome = 0           #ZERO START
        self.maxFitness = 0.0 
        self.measured = 0.0
    

    def newPool(self, xAI):
        self.innovation = xAI.OutputsCount


class newSpecies:
    #NEW SPECIES CLASS
    def __init__(self):
        #INITIALIZE
        self.topFitness = 0.0
        self.staleness = 0
        self.genomes = [] #newGenome 
        self.averageFitness = 0.0


class newGenome: #IComparable<newGenome>
    #NEW GENOM CLASS
    def __init__(self, xAI):
        #INITIALIZE
        self.genes = [] #newGene
        self.fitness = 0.0
        self.adjustedFitness = 0
        self.network = None
        self.maxneuron = 0
        self.globalRank = 0
        self.mutationRates = {} #string, double

        self.mutationRates['connections'] = xAI.MutateConnectionsChance
        self.mutationRates['link'] = xAI.LinkMutationChance
        self.mutationRates['bias'] = xAI.BiasMutationChance
        self.mutationRates['node'] = xAI.NodeMutationChance
        self.mutationRates['enable'] = xAI.EnableMutationChance
        self.mutationRates['disable'] = xAI.DisableMutationChance
        self.mutationRates['step'] = xAI.StepSize


    def __eq__(self, other):
        #COMPARE TO
        return self.fitness == other.fitness

    def __lt__(self, other):
        #ORDERING
        return self.fitness < other.fitness


    def copyGenome(self, xAI):
        #COPY GENOME
        genome = newGenome(xAI)

        for i in range(len(genome.genes)):
            genome.genes.append(genome.genes[i].Copy())

        genome.maxneuron = self.maxneuron
        genome.mutationRates['connections'] = self.mutationRates['connections']
        genome.mutationRates['link'] = self.mutationRates['link']
        genome.mutationRates['bias'] = self.mutationRates['bias']
        genome.mutationRates['node'] = self.mutationRates['node']
        genome.mutationRates['enable'] = self.mutationRates['enable']
        genome.mutationRates['disable'] = self.mutationRates['disable']
        return genome
    

    def basicGenome(self, xAI): #newGenome
        #BASIC GENOME
        genome = newGenome(xAI)

        genome.maxneuron = xAI.InputsCount
        xAI.mutate(genome)

        return genome


class newGene: #IComparable<newGene>
    #NEW GENE CLASS
    def __init__(self):
        #INITIALIZE
        self.into = 0
        self.output = 0
        self.weight = 0.0
        self.enabled = True
        self.innovation = 0

    def __eq__(self, other): #newGene
        #EQUALITY
        return self.output == other.output

    def __lt__(self, other):
        #ORDERING
        return self.output < other.output

    def Copy(self):
        #COPY GENE
        gene = newGene()
        gene.into = self.into
        gene.output = self.output
        gene.weight = self.weight
        gene.enabled = self.enabled
        gene.innovation = self.innovation
        return gene


class newNeuron:
    #NEW NEURON CLASS
    def __init__(self):
        #INITIALIZE
        self.incoming = [] #newGene
        self.value = 0.0
        

class generateNetwork:
    #GENERATE NETWORK CLASS
    def __init__(self, xAI, genome):
        #INITIALIZE
        self.neurons = {} #int, newNeuron

        #INPUTS
        for i in range(xAI.InputsCount):
            self.neurons[i] = newNeuron()            
    
        #OUTPUTS
        for i in range(xAI.OutputsCount):
            self.neurons[xAI.MaxNodes + i] = newNeuron()

        genome.genes = sorted(genome.genes) #table.sort(genome.genes, function(a, b) return (a.output < b.output) end)

        for i in range(len(genome.genes)):
            gene = genome.genes[i]
            
            if (gene.enabled):
            
                if not (gene.output in self.neurons):
                    self.neurons[gene.output] = newNeuron()

                neuron = self.neurons[gene.output]
                neuron.incoming.append(gene)

                if not (gene.into in self.neurons):
                    self.neurons[gene.into] = newNeuron()
            
            genome.network = self
        

    def evaluateNetwork(self, xAI): #List<double>
        #GET OUTPUT AS BOOLEAN
        xAI.Inputs.append(1.0)

        if (len(xAI.Inputs) != xAI.InputsCount):
            print('Incorrect number of neural network inputs...')
            return {}

        for i in range(xAI.InputsCount):
            self.neurons[i].value = xAI.Inputs[i]

        for neuron in self.neurons: #LOOP NEVER INPUTS
            summe = 0.0
            
            for i in range(len(self.neurons[neuron].incoming)):
                incoming = self.neurons[neuron].incoming[i]
                other = self.neurons[incoming.into]
                summe += incoming.weight * other.value                               #WEIGHT CALCULATION

            if (len(self.neurons[neuron].incoming) > 0):
                self.neurons[neuron].value = xAI.Sigmoid(summe)                      #SIGMOID FUNCTION

        outputs = {} #string, bool
        for i in range(xAI.OutputsCount):
            button = xAI.OutputKeys[i]                                               #GET BUTTON NAMES
            
            if (self.neurons[xAI.MaxNodes + i].value > 0): outputs[button] = True    #OUTPUT IS TRUE
            else: outputs[button] = False                                            #OUTPUT IS FALSE

            xAI.Outputs[i] = self.neurons[xAI.MaxNodes + i].value                    #SET OUTPUTS

        return outputs