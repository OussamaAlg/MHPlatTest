﻿
using MHPlatTest.Interfaces;
using MHPlatTest.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHPlatTest.BenchmarkFunctions.CEC2021
{
    /// <summary>
    /// WORKS ONLY WITH D=10 OR D=20 (MANUALLY MODIFY MinProblemDimension and MaxProblemDimension to use appropriate dimension)
    /// the problems are defined in the paper entitled
    /// Problem Definitions and Evaluation Criteria for the CEC 2021     Special Session and Competition on Single Objective Bound Constrained Numerical Optimization
    /// DOI: 10.13140/RG.2.2.36130.66245
    /// </summary>
    internal class CEC21_CompositionFunction2 : IBenchmarkFunction
    {
        public CEC21_CompositionFunction2()
        {
            //Generate unique identifier for current instance
            Random random = new Random();
            ParentInstanceID = random.Next();
        }
        public string Name { get; set; } = "Composition Function 2 CEC21";
        public string Description { get; set; } = "Composition Function 2 with 4 Functions";
        public int IDNumero { get; set; } = 9;
        public double[] SearchSpaceMinValue { get; set; } = new double[1] { -100 };
        public double[] SearchSpaceMaxValue { get; set; } = new double[1] { 100 };
        public short MinProblemDimension { get; set; } = 10;
        public short MaxProblemDimension { get; set; } = 10;
        public int ParentInstanceID { get; set; }

        public double ComputeValue(double[] functionParameter, ref int currentNumberofunctionEvaluation, bool ShiftOptimumToZero)
        {
            //functionParameter.SetDataElementsToSigleValue(1);
            //Increase the current number of function evaluation by 1
            currentNumberofunctionEvaluation++;

            int nbrProblemDimension = functionParameter.Length;
            if (MinProblemDimension == MaxProblemDimension)
            {
                nbrProblemDimension = MinProblemDimension;
            }



            double[] functionParameter1 = new double[(int)nbrProblemDimension];
            double shiftDataValue = -1;
            for (int iShiftData = 0; iShiftData < nbrProblemDimension; iShiftData++)
            {
                functionParameter1[iShiftData] = shiftDataValue + functionParameter[iShiftData];
                if (functionParameter1[iShiftData] > SearchSpaceMaxValue[0])
                    functionParameter1[iShiftData] = SearchSpaceMaxValue[0];
            }


            double result = 0;
            double w_max = 0;
            int cf_num = 4;
            double[] delta = new double[4] { 10, 20, 30, 40 };

            double[] smallOmega = new double[4] { 0, 0, 0, 0 };


            double[] y = new double[nbrProblemDimension];
            double[] z = new double[nbrProblemDimension];
            double resultF1 = 0;
            double resultF2 = 0;
            double resultF3 = 0;
            double resultF4 = 0;
            int tempValue = 0;
            double squareSum = 0;
            double smallOmegaSum = 0;



            Ackley Ackley_func = new Ackley();
            double[] Ackley_funcParameter = new double[nbrProblemDimension];
            for (int i1 = 0; i1 < nbrProblemDimension; i1++)
            {
                Ackley_funcParameter[i1] = 1 + functionParameter1[i1] * 32.768 / 100.0;//Added 1 to remove shift value when calling the function Compute function. Shift value already included in 'functionParameter1' 
            }
            resultF1 = Ackley_func.ComputeValue(Ackley_funcParameter, ref tempValue, ShiftOptimumToZero);
            resultF1 = resultF1 * 10.0;


            HighConditionedElliptic HighConditionedElliptic_func = new HighConditionedElliptic();
            double[] HighConditionedElliptic_funcParameter = new double[nbrProblemDimension];
            for (int i1 = 0; i1 < nbrProblemDimension; i1++)
            {
                HighConditionedElliptic_funcParameter[i1] = 1 + functionParameter1[i1];//Added 1 to remove shift value when calling the function Compute function. Shift value already included in 'functionParameter1' 
            }
            resultF2 = HighConditionedElliptic_func.ComputeValue(HighConditionedElliptic_funcParameter, ref tempValue, ShiftOptimumToZero);
            resultF2 = resultF1 * 1E-06;

            Griewank Griewank_func = new Griewank();
            double[] Griewank_funcParameter = new double[nbrProblemDimension];
            for (int i1 = 0; i1 < nbrProblemDimension; i1++)
            {
                Griewank_funcParameter[i1] = 1 + functionParameter1[i1] * 6.0;//Added 1 to remove shift value when calling the function Compute function. Shift value already included in 'functionParameter1' 
            }
            resultF3 = Griewank_func.ComputeValue(Griewank_funcParameter, ref tempValue, ShiftOptimumToZero);
            resultF3 = resultF3 * 10.0;


            Rastrigin Rastrigin_func = new Rastrigin();
            double[] Rastrigin_funcParameter = new double[nbrProblemDimension];
            for (int i1 = 0; i1 < nbrProblemDimension; i1++)
            {
                Rastrigin_funcParameter[i1] = 0.1 + functionParameter1[i1] * 5.12 / 100.0;//Added 1 to remove shift value when calling the function Compute function. Shift value already included in 'functionParameter1' 
            }
            resultF4 = Rastrigin_func.ComputeValue(Rastrigin_funcParameter, ref tempValue, ShiftOptimumToZero);


            //COmputing smallOmega values
            for (int i1 = 0; i1 < nbrProblemDimension; i1++)
            {
                squareSum += functionParameter1[i1] * functionParameter1[i1];
            }

            for (int i1 = 0; i1 < cf_num; i1++)
            {
                if (squareSum != 0)
                {
                    smallOmega[i1] = (1 / Math.Sqrt(squareSum)) * Math.Exp(-squareSum / (2 * nbrProblemDimension * Math.Pow(delta[i1], 2)));
                }
                else
                {
                    smallOmega[i1] = double.MaxValue / (double)(cf_num + 1.0);
                }

                if (smallOmega[i1] > w_max)
                {
                    w_max = smallOmega[i1];
                }
                smallOmegaSum += smallOmega[i1];

            }

            if (w_max == 0)
            {
                for (int i1 = 0; i1 < cf_num; i1++)
                {
                    smallOmega[i1] = 1;
                }
                smallOmegaSum = cf_num;
            }


            for (int i1 = 0; i1 < cf_num; i1++)
            {
                smallOmega[i1] = smallOmega[i1] / smallOmegaSum;
            }


            result = resultF1 * smallOmega[0] + resultF2 * smallOmega[1] + resultF3 * smallOmega[2] + resultF4 * smallOmega[3];

            //Shift the optimum if it is required
            if (ShiftOptimumToZero == true)
            {
                return result - OptimalFunctionValue(functionParameter1.Length);
            }

            return (result);
        }

        public double OptimalFunctionValue(int nbrProblemDimension)
        {
            if (MinProblemDimension == MaxProblemDimension)
            {
                nbrProblemDimension = MinProblemDimension;
            }

            double tempResult = 0;

            return tempResult;
        }


        public List<double[]> OptimalPoint(int nbrProblemDimension)
        {
            if (MinProblemDimension == MaxProblemDimension)
            {
                nbrProblemDimension = MinProblemDimension;
            }

            double[] tempResult = new double[nbrProblemDimension];
            for (int i = 0; i < nbrProblemDimension; i++)
            {
                tempResult[i] = 0;
            }

            return new List<double[]> { tempResult };

        }

    }
}

