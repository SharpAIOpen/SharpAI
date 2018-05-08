using Core.Classes;
using Core.Modifications;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;


/*############################################################################*
 *                          Perzeptron Algorithm                              *
 *                 https://de.wikipedia.org/wiki/Perzeptron                   *
 *                    writen in c# by Martin Kober 2017                       *
 *############################################################################*/


namespace NeuralNet.Project
{
    public enum MODE
    {
        //MODE ENUM
        PIXEL, HSENSOR
    }

    public class NetNeural
    {
        public Size SizeNet = new Size(28, 28);
        public Size SizeScale = new Size(140, 140);
        public double[,] weightInput;
        public double[,] weightOutput;
        public int PixelWidth;
        public int PixelHeight;
        public int NodesInput;
        public int NodesHidden;
        public int NodesOutput;
        public double LearningRate;
        public MODE Mode;
        public double[] AnswerArray;
        public int Answer;
        Random Random = new Random();

        public NetNeural(int xPixelWidth, int xPixelHeight, int xHiddenNodes, int xOutputNodes, double xLearningRate, MODE xMode)
        {
            //SWITCH MODE
            int netWidth = xPixelWidth, netHeight = xPixelHeight;
            switch (xMode)
            {
                case MODE.HSENSOR:
                    netWidth = 1; //ONE LINE OF SENSORS
                    break;
            }

            //TRANSFER VARIABLES
            PixelWidth = xPixelWidth;
            PixelHeight = xPixelHeight;
            NodesInput = netWidth * netHeight;
            NodesHidden = xHiddenNodes;
            NodesOutput = xOutputNodes;
            LearningRate = xLearningRate;
            Mode = xMode;

            //SET MAIN SIZE
            SizeNet = new Size(PixelWidth, PixelHeight);
            SizeScale = Mod_PNG.getScale(SizeNet, new Size(140, 140), true); //KEEP RELATIONS

            //INITIALIZE WEIGHTS
            initWeights();
        }

        public static MODE getMode(int xIndex)
        {
            //GET MODE BY INTEGER
            object[] ModeList = Mod_Convert.EnumToList(typeof(MODE));
            return (MODE)ModeList[xIndex];
        }

        private void initWeights()
        {
            //INITIALIZE WEIGHTS
            double mean = 0.0;

            //INPUT WEIGHTS
            weightInput = new double[NodesHidden, NodesInput];
            fillWeights(weightInput, NodesHidden, NodesInput, mean, Math.Pow(NodesHidden, -0.5));

            //OUTPUT WEIGHTS
            weightOutput = new double[NodesOutput, NodesHidden];
            fillWeights(weightOutput, NodesOutput, NodesHidden, mean, Math.Pow(NodesOutput, -0.5));
        }

        private void fillWeights(double[,] xMatrix, int xRows, int xColumns, double xMean, double xStabw)
        {
            //FILL WEIGHTS
            for (int j = 0; j < xRows; j++)
                for (int i = 0; i < xColumns; i++)
                    xMatrix[j, i] = SampleGaussian(Random, xMean, xStabw);
        }

        public bool Exist()
        {
            //EXIST NEURAL NETWORK
            if (this != null) return true;
            else return false;
        }

        public void Dispose()
        {
            //DISPOSE NEURAL NETWORK
            Dispose();
        }

        public void Train(double[,] xInputs, double[,] xTargets)
        {
            //TRAIN NEURAL NETWORK
            xInputs = NormalizeInput(xInputs);
            double[,] inputs = Transpose(xInputs);
            double[,] targets = Transpose(xTargets);

            //CALCULATE SIGNALS INTO HIDDEN LAYER
            double[,] hiddenInputs = Multiply(weightInput, inputs);
            double[,] hiddenOutputs = ExpitFunction(hiddenInputs);

            //CALCULATE SIGNALS INTO FINAL OUTPUT LAYER
            double[,] finalInputs = Multiply(weightOutput, hiddenOutputs);
            double[,] finalOutputs = ExpitFunction(finalInputs);

            //OUTPUT LAYER ERROR IS THE (TARGET - ACTUAL)
            double[,] errorOutput = Subtract(targets, finalOutputs);
            double[,] errorHidden = Multiply(Transpose(weightOutput), errorOutput);

            //weightOutput += learnrate * ((errorOutput * finalOutputs * (1.0 - finalOutputs)) * Transpose(hiddenOutputs))
            weightOutput = Addition(weightOutput, Multiply(LearningRate, Multiply(Multiply(Multiply(errorOutput, finalOutputs), Subtract(1.0, finalOutputs)), Transpose(hiddenOutputs))));

            //weightInput += learnrate * ((errorHidden * hiddenOutputs * (1.0 - hiddenOutputs)) * Transpose(inputs))
            weightInput = Addition(weightInput, Multiply(LearningRate, Multiply(Multiply(Multiply(errorHidden, hiddenOutputs), Subtract(1.0, hiddenOutputs)), Transpose(inputs))));
        }

        public double[] Query(double[,] xInputs)
        {
            //QUERY NEURAL NETWORK
            xInputs = NormalizeInput(xInputs);
            double[,] inputs = Transpose(xInputs);

            //CALCULATE SIGNALS INTO HIDDEN LAYER
            double[,] hiddenInputs = Multiply(weightInput, inputs);
            if (hiddenInputs == null) { UniMsg.Show("Error Matrix Dimensions...", "Matrix must have the same size", MessageBoxButtons.OK); return null; }
            double[,] hiddenOutputs = ExpitFunction(hiddenInputs);

            //CALCULATE SIGNALS INTO FINAL OUTPUT LAYER
            double[,] finalInputs = Multiply(weightOutput, hiddenOutputs);
            double[,] finalOutputs = ExpitFunction(finalInputs);

            AnswerArray = finalOutputs.Cast<double>().ToArray();
            Answer = Array.IndexOf(AnswerArray, AnswerArray.Max());
            return AnswerArray;
        }

        public double[] QueryBack(double[,] xTargets)
        {
            //BACK QUERY NEURAL NETWORK
            double[,] finalOutputs = Transpose(xTargets);
            double[,] finalInputs = LogitFunction(finalOutputs);

            //CALCULATE THE SIGNAL OUT OF THE HIDDEN LAYER
            double[,] hiddenOutputs = Multiply(Transpose(weightOutput), finalInputs);
            hiddenOutputs = Subtract(hiddenOutputs, MatrixMinimum(hiddenOutputs));
            hiddenOutputs = Division(hiddenOutputs, MatrixMaximum(hiddenOutputs));
            hiddenOutputs = Multiply(0.98, hiddenOutputs);
            hiddenOutputs = Addition(0.01, hiddenOutputs);

            //CALCULATE THE SIGNAL INTO THE HIDEEN LAYER
            double[,] hiddenInputs = LogitFunction(hiddenOutputs);

            //CALCULATE THE SIGNAL OUT OF THE INPUT LAYER
            double[,] inputs = Multiply(Transpose(weightInput), hiddenInputs);
            inputs = Subtract(inputs, MatrixMinimum(inputs));
            inputs = Division(inputs, MatrixMaximum(inputs));
            inputs = Multiply(0.98, inputs);
            inputs = Addition(0.01, inputs);

            return inputs.Cast<double>().ToArray();
        }

        private static double MatrixMinimum(double[,] xMatrix)
        {
            //GET MATRIX MINIMUM
            int rows = xMatrix.GetLength(0), columns = xMatrix.GetLength(1);
            double minimum = double.MaxValue;

            //FIND MINIMUM
            for (int j = 0; j < rows; j++)
                for (int i = 0; i < columns; i++)
                    if (xMatrix[j, i] < minimum)
                        minimum = xMatrix[j, i];
            return minimum;
        }

        private static double MatrixMaximum(double[,] xMatrix)
        {
            //GET MATRIX MAXIMUM
            int rows = xMatrix.GetLength(0), columns = xMatrix.GetLength(1);
            double maximum = double.MinValue;

            //FIND MAXIMUM
            for (int j = 0; j < rows; j++)
                for (int i = 0; i < columns; i++)
                    if (xMatrix[j, i] > maximum)
                        maximum = xMatrix[j, i];
            return maximum;
        }

        private static double[,] NormalizeInput(double[,] xMatrix)
        {
            //NORMALIZE INPUT
            int rows = xMatrix.GetLength(0), columns = xMatrix.GetLength(1);
            double maximum = MatrixMaximum(xMatrix);

            //NORMALIZE INPUT BETWEEN 0 AND 1
            if (maximum > 1)
                for (int j = 0; j < rows; j++)
                    for (int i = 0; i < columns; i++)
                        xMatrix[j, i] = (xMatrix[j, i] / maximum * 0.99) + 0.01;
            return xMatrix;
        }

        private static double SampleGaussian(Random random, double mean, double stddev)
        {
            //CREATE RANDOM GAUSSIAN DISTRIBUTION
            double x1 = 1 - random.NextDouble();
            double x2 = 1 - random.NextDouble();

            double y1 = Math.Sqrt(-2.0 * Math.Log(x1)) * Math.Cos(2.0 * Math.PI * x2);
            return (y1 * stddev) + mean;
        }

        private double[,] ExpitFunction(double[,] xMatrix)
        {
            //SIGMOID FUNCTION expit(x) = 1/(1+exp(-x))
            int rows = xMatrix.GetLength(0), columns = xMatrix.GetLength(1);

            for (int j = 0; j < rows; j++)
                for (int i = 0; i < columns; i++)
                    xMatrix[j, i] = 1 / (1 + Math.Exp(-xMatrix[j, i]));

            return xMatrix;
        }

        private double[,] LogitFunction(double[,] xMatrix)
        {
            //REVERSE SIGMOID FUNCTION logit(p) = log(p/(1-p))
            int rows = xMatrix.GetLength(0), columns = xMatrix.GetLength(1);

            for (int j = 0; j < rows; j++)
                for (int i = 0; i < columns; i++)
                    xMatrix[j, i] = Math.Log(xMatrix[j, i] / (1 - xMatrix[j, i]));

            return xMatrix;
        }

        private double[,] Transpose(double[,] xMatrix)
        {
            //TRANSPOSE A MATRIX
            int w = xMatrix.GetLength(0);
            int h = xMatrix.GetLength(1);

            double[,] result = new double[h, w];

            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                    result[j, i] = xMatrix[i, j];

            return result;
        }

        enum OPERATION
        {
            //MATRIX OPERATION ENUM
            ADDITION, SUBTRACTION, MULTIPLICATION, DIVISION
        }

        private static double[,] Operation(double[,] xMatrixA, double[,] xMatrixB, OPERATION xOperation)
        {
            //MATRIX OPERATIONS WITH TWO MATRIX
            if (xMatrixA.GetLength(1) == xMatrixB.GetLength(0)) //CROSS OPERATION
            {
                double[,] matrix = new double[xMatrixA.GetLength(0), xMatrixB.GetLength(1)];
                for (int j = 0; j < matrix.GetLength(0); j++)
                    for (int i = 0; i < matrix.GetLength(1); i++)
                    {
                        matrix[j, i] = 0;
                        for (int k = 0; k < xMatrixA.GetLength(1); k++)
                            switch (xOperation)
                            {
                                case OPERATION.MULTIPLICATION:
                                    matrix[j, i] = matrix[j, i] + xMatrixA[j, k] * xMatrixB[k, i];
                                    break;
                                case OPERATION.DIVISION:
                                    matrix[j, i] = matrix[j, i] + xMatrixA[j, k] / xMatrixB[k, i];
                                    break;
                            }
                    }
                return matrix;
            }
            else if (xMatrixA.GetLength(0) == xMatrixB.GetLength(0) && xMatrixA.GetLength(1) == xMatrixB.GetLength(1))
            {
                double[,] matrix = new double[xMatrixA.GetLength(0), xMatrixA.GetLength(1)];
                for (int i = 0; i < matrix.GetLength(0); i++)
                    for (int j = 0; j < matrix.GetLength(1); j++)
                        switch (xOperation)
                        {
                            case OPERATION.ADDITION:
                                matrix[i, j] = xMatrixA[i, j] + xMatrixB[i, j];
                                break;
                            case OPERATION.SUBTRACTION:
                                matrix[i, j] = xMatrixA[i, j] - xMatrixB[i, j];
                                break;
                            case OPERATION.MULTIPLICATION:
                                matrix[i, j] = xMatrixA[i, j] * xMatrixB[i, j];
                                break;
                            case OPERATION.DIVISION:
                                matrix[i, j] = xMatrixA[i, j] / xMatrixB[i, j];
                                break;
                        }
                return matrix;
            }
            else
            { Console.WriteLine("\n Number of columns in First Matrix should be equal to Number of rows in Second Matrix.\n Please re-enter correct dimensions."); return null; }
        }

        private static double[,] Operation(double xDouble, double[,] xMatrixA, OPERATION xOperation)
        {
            //MATRIX OPERATIONS WITH ONE DOUBLE AND ONE MATRIX
            double[,] matrix = new double[xMatrixA.GetLength(0), xMatrixA.GetLength(1)];
            for (int j = 0; j < matrix.GetLength(0); j++)
                for (int i = 0; i < matrix.GetLength(1); i++)
                    switch (xOperation)
                    {
                        case OPERATION.ADDITION:
                            matrix[j, i] = xDouble + xMatrixA[j, i];
                            break;
                        case OPERATION.SUBTRACTION:
                            matrix[j, i] = xDouble - xMatrixA[j, i];
                            break;
                        case OPERATION.MULTIPLICATION:
                            matrix[j, i] = xDouble * xMatrixA[j, i];
                            break;
                        case OPERATION.DIVISION:
                            matrix[j, i] = xDouble / xMatrixA[j, i];
                            break;
                    }
            return matrix;
        }

        private static double[,] Operation(double[,] xMatrixA, double xDouble, OPERATION xOperation)
        {
            //MATRIX OPERATIONS WITH ONE DOUBLE AND ONE MATRIX
            double[,] matrix = new double[xMatrixA.GetLength(0), xMatrixA.GetLength(1)];
            for (int j = 0; j < matrix.GetLength(0); j++)
                for (int i = 0; i < matrix.GetLength(1); i++)
                    switch (xOperation)
                    {
                        case OPERATION.ADDITION:
                            matrix[j, i] = xMatrixA[j, i] + xDouble;
                            break;
                        case OPERATION.SUBTRACTION:
                            matrix[j, i] = xMatrixA[j, i] - xDouble;
                            break;
                        case OPERATION.MULTIPLICATION:
                            matrix[j, i] = xMatrixA[j, i] * xDouble;
                            break;
                        case OPERATION.DIVISION:
                            matrix[j, i] = xMatrixA[j, i] / xDouble;
                            break;
                    }
            return matrix;
        }

        private static double[,] Addition(double[,] xMatrixA, double[,] xMatrixB)
        {
            //ADD TWO MATRIX
            return Operation(xMatrixA, xMatrixB, OPERATION.ADDITION);
        }

        private static double[,] Addition(double xDouble, double[,] xMatrixA)
        {
            //ADD ONE DOUBLE AND ONE MATRIX
            return Operation(xDouble, xMatrixA, OPERATION.ADDITION);
        }

        private static double[,] Addition(double[,] xMatrixA, double xDouble)
        {
            //ADD ONE MATRIX AND ONE DOUBLE
            return Operation(xMatrixA, xDouble, OPERATION.ADDITION);
        }

        private static double[,] Subtract(double[,] xMatrixA, double[,] xMatrixB)
        {
            //SUBTRACT TWO MATRIX
            return Operation(xMatrixA, xMatrixB, OPERATION.SUBTRACTION);
        }

        private static double[,] Subtract(double xDouble, double[,] xMatrixA)
        {
            //SUBTRACT ONE DOUBLE AND ONE MATRIX
            return Operation(xDouble, xMatrixA, OPERATION.SUBTRACTION);
        }

        private static double[,] Subtract(double[,] xMatrixA, double xDouble)
        {
            //SUBTRACT ONE MATRIX AND ONE DOUBLE
            return Operation(xMatrixA, xDouble, OPERATION.SUBTRACTION);
        }

        private static double[,] Multiply(double[,] xMatrixA, double[,] xMatrixB)
        {
            //MULTIPLY TWO MATRIX
            return Operation(xMatrixA, xMatrixB, OPERATION.MULTIPLICATION);
        }

        private static double[,] Multiply(double xDouble, double[,] xMatrixA)
        {
            //MULTIPLY ONE DOUBLE AND ONE MATRIX
            return Operation(xDouble, xMatrixA, OPERATION.MULTIPLICATION);
        }

        private static double[,] Division(double[,] xMatrixA, double xDouble)
        {
            //DIVISION ONE MATRIX AND ONE DOUBLE
            return Operation(xMatrixA, xDouble, OPERATION.DIVISION);
        }
    }
}
