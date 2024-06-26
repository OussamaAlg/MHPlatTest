// See https://aka.ms/new-console-template for more information


using MHPlatTest.Algorithms;
using MHPlatTest.Divers;
using MHPlatTest.Interfaces;
using MHPlatTest.Models;
using MHPlatTest.Utility;
using System.Reflection;
//using Newtonsoft.Json;
using System.Text.Json;
using System.Runtime.InteropServices;
using MHPlatTest.BenchmarkFunctions.CEC2021;
using System.Linq.Expressions;
using MHPlatTest.BenchmarkFunctions;
using MHPlatTest.BenchmarkFunctions.FixedDimension;

namespace MHPlatTest
{

    public enum EXECUTION_STATE : uint
    {
        ES_AWAYMODE_REQUIRED = 0x00000040,
        ES_CONTINUOUS = 0x80000000,
        ES_DISPLAY_REQUIRED = 0x00000002,
        ES_SYSTEM_REQUIRED = 0x00000001
    }

    internal class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);
    }

    //...


    public class Program
    {




        static void Main()
        {

            //Flag for parallel executions
            bool runOneProcessOnlyFlag = false;

#if DEBUG
            runOneProcessOnlyFlag = true;
#endif



            Random randomGenerator = new Random();
            Program mainProgram = new();


            //Constructing the differents configuration
            List<OptimizationParameter> MHConf;
            MHConf = mainProgram.DoOptimizationProcessConfiguration();

            string executionTimeStamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
            string pathFolderForResults = AppContext.BaseDirectory + "\\" + executionTimeStamp + "\\";

            //Configure the number of independent runs
            int numberRepetition =100;

            //Loading the desired benchmark functions
            List<IBenchmarkFunction> TempUsedBenchmarkFunctionList = new();
            List<List<IBenchmarkFunction>> usedBenchmarkFunctionList = new();
            List<string> benchmarkFunctionNameToIncludeList = new List<string>();
            List<string> benchmarkFunctionNameToIgnoreList = new List<string>() { "CEC21", "FixedDimension" };

            //Add only scalable benchmark functions
            TempUsedBenchmarkFunctionList = LoadBenchmarkFunctions(benchmarkFunctionNameToIgnoreList, benchmarkFunctionNameToIncludeList);

            //You can add custom benchmarks
            //TempUsedBenchmarkFunctionList = new() { new Rastrigin(), new Booth() };


            usedBenchmarkFunctionList.Add(TempUsedBenchmarkFunctionList);





            #region CSV File configuration in disk
            //Creating folder if not existing
            if (Directory.Exists(pathFolderForResults) == false)
            {
                Directory.CreateDirectory(pathFolderForResults);
            }

            //Saving the CSV file into disk
            string pathCSVFile;
            int counterCSVFile = 1;
            pathCSVFile = pathFolderForResults + "ResultsByDim" + counterCSVFile + ".csv";
            while (File.Exists(pathCSVFile) == true)
            {
                pathCSVFile = pathFolderForResults + "ResultsByDim" + counterCSVFile++ + ".csv";
            }


            string pathBestMeanEvalCSVFile;
            int counterBestMeanEvalCSVFile = 1;
            pathBestMeanEvalCSVFile = pathFolderForResults + "BestMeanEvalData" + counterBestMeanEvalCSVFile + ".csv";
            while (File.Exists(pathBestMeanEvalCSVFile) == true)
            {
                pathBestMeanEvalCSVFile = pathFolderForResults + "BestMeanEvalData" + counterBestMeanEvalCSVFile++ + ".csv";
            }

            #endregion


            //Configure the stats to be displayed during execution on the terminal window
            List<Tuple<MHOptimizationResult, StatsToComputeType>> StatsToComputeList = new();

            //Configure the stats to be saved in a CSV file in disk
            List<Tuple<MHOptimizationResult, StatsToComputeType>> csvFile_StatsToComputeList = new();


            #region Stats for regular Optimization results
            //Configure the stats to be dusplayed during execution on the terminal window
            StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.OptimalFunctionValue, StatsToComputeType.Mean));
            StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.OptimalFunctionValue, StatsToComputeType.STD));
            StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.NumberOfFunctionEvaluation, StatsToComputeType.Mean));
            StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.NumberOfTotalIteration, StatsToComputeType.Mean));
            StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.ExecutionTime, StatsToComputeType.Mean));

            //Configure the stats to be saved in a CSV file in disk
            csvFile_StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.OptimalFunctionValue, StatsToComputeType.Mean));
            csvFile_StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.OptimalFunctionValue, StatsToComputeType.Max));
            csvFile_StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.OptimalFunctionValue, StatsToComputeType.Min));
            csvFile_StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.OptimalFunctionValue, StatsToComputeType.STD));
            csvFile_StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.OptimalFunctionValue, StatsToComputeType.Median));
            csvFile_StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.NumberOfFunctionEvaluation, StatsToComputeType.Mean));
            csvFile_StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.NumberOfTotalIteration, StatsToComputeType.Mean));
            csvFile_StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.ExecutionTime, StatsToComputeType.Mean));

            #endregion


            #region Stats for 'Number Function evaluations' and 'Successful rate of convergence '
            ////Configure the stats to be dusplayed during execution on the terminal window
            //StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.NumberOfFunctionEvaluation, StatsToComputeType.Mean));
            //StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.NumberOfFunctionEvaluation, StatsToComputeType.STD));
            //StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.NumberOfFunctionEvaluation, StatsToComputeType.Mean));
            //StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.OptimumFound, StatsToComputeType.Mean));
            //StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.OptimumFound, StatsToComputeType.Min));
            //StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.OptimumFound, StatsToComputeType.Max));

            ////Configure the stats to be saved in a CSV file in disk
            //csvFile_StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.NumberOfFunctionEvaluation, StatsToComputeType.Mean));
            //csvFile_StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.NumberOfFunctionEvaluation, StatsToComputeType.Max));
            //csvFile_StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.NumberOfFunctionEvaluation, StatsToComputeType.Min));
            //csvFile_StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.OptimumFound, StatsToComputeType.Mean));
            //csvFile_StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.OptimumFound, StatsToComputeType.Max));
            //csvFile_StatsToComputeList.Add(new Tuple<MHOptimizationResult, StatsToComputeType>(MHOptimizationResult.OptimumFound, StatsToComputeType.Min));

            #endregion



            List<Tuple<string, List<double>>> StatsForCSV = new();
            List<List<Tuple<string, List<double>>>> ListStatsForCSV = new();
            List<List<Tuple<string, List<double>>>> StatsByMHAlgoAndBenchFunc = new();
            string contentCSVFile = "";



            #region Choosing the MH algorithm and runing the tests
            List<string> algoToIgnore = new List<string>();
            List<string> metaheuristicNameToIgnoreList = new List<string>();
            List<string> metaheuristicNameToIncludeList = new List<string>();
            List<string> configurationTextDetailsList = new List<string>();
            List<IMHAlgorithm> usedMetaheuristicAlgorithmList = new List<IMHAlgorithm>();
            //usedMetaheuristicAlgorithmList = LoadMetaheuristicOptimizationAlgorithms(MHConf, randomGenerator, metaheuristicNameToIgnoreList, metaheuristicNameToIncludeList);

            //This object contains the results of all conducted tests
            List<List<GlobalBatchResultModel>?> GlobalResults = new();

            int dimensionUsed = 0;
            //Configuring the tests for scalable benchmark functions
            for (int i = 0; i < 3; i++)
            {
                usedMetaheuristicAlgorithmList = new List<IMHAlgorithm>();

                //The tests will be repeated for Dimension=10, dimension=30 and Dimension=50
                switch (i)
                {
                    case 0:
                        dimensionUsed = 10;
                        break;
                    case 1:
                        dimensionUsed = 30;
                        break;
                    case 2:
                        dimensionUsed = 50;
                        break;
                    default:
                        break;
                }



                #region Adding and configuring optimization algorithms to be included in the tests
                //Configure the design parameters for all optimization algorithms
                MHConf.Where(x => x.Name == MHAlgoParameters.StopOptimizationWhenOptimumIsReached).First().Value = true;
                MHConf.Where(x => x.Name == MHAlgoParameters.PopulationSize).First().Value = 40;
                MHConf.Where(x => x.Name == MHAlgoParameters.ABC_LimitValue).First().Value = 100;
                MHConf.Where(x => x.Name == MHAlgoParameters.ProblemDimension).First().Value = dimensionUsed;
                MHConf.Where(x => x.Name == MHAlgoParameters.FunctionValueSigmaTolerance).First().Value = 1e-16;
                MHConf.Where(x => x.Name == MHAlgoParameters.StoppingCriteriaType).First().Value = StoppingCriteriaType.MaximalNumberOfFunctionEvaluation;
                MHConf.Where(x => x.Name == MHAlgoParameters.MaxFunctionEvaluationNumber).First().Value = 100000;


                // Adding the optimization algorithm to be included in the tests
                usedMetaheuristicAlgorithmList.Add(new DirectedABC(MHConf, "", randomGenerator.Next()));  // By Kıran and Fındık (2015)
                usedMetaheuristicAlgorithmList.Add(new BasicABC(MHConf, "", randomGenerator.Next()));  // By Karaboga (2005)
                usedMetaheuristicAlgorithmList.Add(new ImprovedABCAdaptiveMingZhao(MHConf, "", randomGenerator.Next())); // By Zhao, Song, and Xing (2022)
                usedMetaheuristicAlgorithmList.Add(new GBestABC(MHConf, "", randomGenerator.Next()));  // By Zhu and Kwong (2010

                //Configuring and adding the MABC algorithm by Akay and Karaboga (2012)
                MHConf.Where(x => x.Name == MHAlgoParameters.MABC_LimitValue).First().Value = 200;
                MHConf.Where(x => x.Name == MHAlgoParameters.MABC_ModificationRate).First().Value = 0.4d;
                MHConf.Where(x => x.Name == MHAlgoParameters.MABC_UseScalingFactor).First().Value = true;
                usedMetaheuristicAlgorithmList.Add(new MABC(MHConf, "MABC Limit200 ScaFactor MR.4", randomGenerator.Next()));

                //Configuring and adding the ARABC algorithm by Cui et al. (2017)
                MHConf.Where(x => x.Name == MHAlgoParameters.ABC_LimitValue).First().Value = dimensionUsed * (int)MHConf.Where(x => x.Name == MHAlgoParameters.PopulationSize).First().Value;
                usedMetaheuristicAlgorithmList.Add(new ARABC(MHConf, "", randomGenerator.Next()));
                MHConf.Where(x => x.Name == MHAlgoParameters.ABC_LimitValue).First().Value = 100;



                //Configuring and adding the proposed 'Adaptive ABC' to be included in the tests             
                //Tuning Cycle (TC) parameter
                MHConf.Where(x => x.Name == MHAlgoParameters.AEEABC_NumberOfIterationsToTuneParameters).First().Value = 3;

                //Tune the number of optimization parameters (dimensions) to mutate when generating new solutions
                MHConf.Where(x => x.Name == MHAlgoParameters.AEEABC_TuneNumberOfDimensionUsingGBest).First().Value = true;
                //The initial number of optimization parameters (dimensions) to mutate equal =1
                MHConf.Where(x => x.Name == MHAlgoParameters.AEEABC_InitialNumberOfDimensionUsingGBestRatio).First().Value = 0d;


                //Tuning the Scout bee generation strategy
                MHConf.Where(x => x.Name == MHAlgoParameters.AEEABC_TuneScoutGenerationType).First().Value = true;
                MHConf.Where(x => x.Name == MHAlgoParameters.ScoutGeneration).First().Value = ScoutGenerationType.Random;

                //Tuning the probability equation
                MHConf.Where(x => x.Name == MHAlgoParameters.AEEABC_TuneProbabilityEquationType).First().Value = true;
                MHConf.Where(x => x.Name == MHAlgoParameters.ABC_ProbabilityEquationType).First().Value = ABC_ProbabilityEquationType.Original;


                //Coeff3 is the 'C' parameter in the update equation of the algorithm
                MHConf.Where(x => x.Name == MHAlgoParameters.AEEABC_TuneCoeff3Value).First().Value = true;
                MHConf.Where(x => x.Name == MHAlgoParameters.AEEABC_Coeff3InitialValue).First().Value = 0d;
                MHConf.Where(x => x.Name == MHAlgoParameters.AEEABC_Coeff3ValueChange).First().Value = 1d;

                usedMetaheuristicAlgorithmList.Add(new AdaABC(MHConf, " It3  (GbestDim From1;Coeff3 (0;1;Max2) ; ScoutType Rdm->Meansum; Proba Ori->Compl)", randomGenerator.Next()));

                #endregion



                //Starting the optimization process for the scalable benchmark functions with the current selected dimension
                GlobalResults.Add(mainProgram.StartOptimizationProcess(usedMetaheuristicAlgorithmList, usedBenchmarkFunctionList[0], numberRepetition, randomGenerator, runOneProcessOnlyFlag));
                configurationTextDetailsList.Add(MHConf.MHConfig2String());

                //Saving optimization results
                StatsByMHAlgoAndBenchFunc.Add(ComputeAndDisplayStats(GlobalResults[i], algoToIgnore, StatsToComputeList));


                //Saving stats to be included in the final CSV file
                StatsForCSV = DiversExtendedProperties.ComputeStat(GlobalResults[GlobalResults.Count - 1], csvFile_StatsToComputeList, true, algoToIgnore, GroupByType.Algorithm, OrderingType.None);
                ListStatsForCSV.Add(StatsForCSV);


                contentCSVFile += "Configuration" + Environment.NewLine + configurationTextDetailsList[GlobalResults.Count - 1] + Environment.NewLine + "Results" + Environment.NewLine + Environment.NewLine;
                contentCSVFile += DiversExtendedProperties.FramtStatsCSVFile(StatsForCSV, csvFile_StatsToComputeList);

                contentCSVFile += Environment.NewLine;
                contentCSVFile += Environment.NewLine;
                contentCSVFile += Environment.NewLine;
                contentCSVFile += Environment.NewLine;
                contentCSVFile += Environment.NewLine;

                //Saving results to disk
                try
                {
                    File.AppendAllText(pathCSVFile, contentCSVFile);
                }
                catch (Exception)
                {
                    pathCSVFile = pathFolderForResults + "ResultsByDim" + counterCSVFile + ".csv";
                    while (File.Exists(pathCSVFile) == true)
                    {
                        pathCSVFile = pathFolderForResults + "ResultsByDim" + counterCSVFile++ + ".csv";
                    }
                    File.AppendAllText(pathCSVFile, contentCSVFile);
                }

                contentCSVFile = "";
            }


            #region  Runing test fo the Fixed dimension benchmark functions
            //Selecting only the fixed dimension benchmark functions
            benchmarkFunctionNameToIgnoreList = new List<string>() { "CEC21", "NotToBeUsed" };
            benchmarkFunctionNameToIncludeList = new List<string>() { "FixedDimension" };
            TempUsedBenchmarkFunctionList = LoadBenchmarkFunctions(benchmarkFunctionNameToIgnoreList, benchmarkFunctionNameToIncludeList);
            usedBenchmarkFunctionList.Add(TempUsedBenchmarkFunctionList);

            //Starting the optimization process
            GlobalResults.Add(mainProgram.StartOptimizationProcess(usedMetaheuristicAlgorithmList, usedBenchmarkFunctionList[usedBenchmarkFunctionList.Count - 1], numberRepetition, randomGenerator, runOneProcessOnlyFlag));
            configurationTextDetailsList.Add(MHConf.MHConfig2String());
            #endregion


            #region  Runing test fo the CEC21 benchmark functions
            //Fixing the dimension problem at 10 as required by the CEC21 functions
            usedMetaheuristicAlgorithmList.ForEach(x => x.OptimizationConfiguration.Where(y => y.Name == MHAlgoParameters.ProblemDimension).First().Value = 10);

            //Selecting only the CEC21 benchmark functions 
            benchmarkFunctionNameToIgnoreList = new List<string>() { "NotToBeUsed" };
            benchmarkFunctionNameToIncludeList = new List<string>() { "CEC21" };
            TempUsedBenchmarkFunctionList = LoadBenchmarkFunctions(benchmarkFunctionNameToIgnoreList, benchmarkFunctionNameToIncludeList);
            TempUsedBenchmarkFunctionList = new List<IBenchmarkFunction> { new CEC21_BentCigar(), new CEC21_schwefel(), new CEC21_Lunacek_bi_Rastrigin(), new CEC21_GriewankRosenbrock(), new CEC21_HybridFunction01(), new CEC21_HybridFunction02(), new CEC21_HybridFunction03(), new CEC21_CompositionFunction1(), new CEC21_CompositionFunction2(), new CEC21_CompositionFunction3() };
            usedBenchmarkFunctionList.Add(TempUsedBenchmarkFunctionList);

            //Starting the optimization process
            GlobalResults.Add(mainProgram.StartOptimizationProcess(usedMetaheuristicAlgorithmList, usedBenchmarkFunctionList[usedBenchmarkFunctionList.Count - 1], numberRepetition, randomGenerator, runOneProcessOnlyFlag));
            configurationTextDetailsList.Add(MHConf.MHConfig2String());
            #endregion







            #endregion




            //Start saving results for only the last two test which include the fixed dimension and CEC21 benchmark functions
            for (int i = GlobalResults.Count - 2; i < GlobalResults.Count; i++)
            {
                //Printing the used confguration
                Console.WriteLine("Configuration");
                Console.WriteLine(configurationTextDetailsList[i]);
                Console.WriteLine("");
                Console.WriteLine("Results");

                StatsByMHAlgoAndBenchFunc.Add(ComputeAndDisplayStats(GlobalResults[i], algoToIgnore, StatsToComputeList));


                //Saving stats to be included in the final CSV file
                StatsForCSV = DiversExtendedProperties.ComputeStat(GlobalResults[i], csvFile_StatsToComputeList, true, algoToIgnore, GroupByType.Algorithm, OrderingType.None);
                ListStatsForCSV.Add(StatsForCSV);

                contentCSVFile += "Configuration" + Environment.NewLine + configurationTextDetailsList[i] + Environment.NewLine + "Results" + Environment.NewLine + Environment.NewLine;
                contentCSVFile += DiversExtendedProperties.FramtStatsCSVFile(StatsForCSV, csvFile_StatsToComputeList);

                contentCSVFile += Environment.NewLine;
                contentCSVFile += Environment.NewLine;
                contentCSVFile += Environment.NewLine;
                contentCSVFile += Environment.NewLine;
                contentCSVFile += Environment.NewLine;

                //Saving results to disk
                try
                {
                    File.AppendAllText(pathCSVFile, contentCSVFile);
                }
                catch (Exception)
                {
                    pathCSVFile = pathFolderForResults + "ResultsByDim" + counterCSVFile + ".csv";
                    while (File.Exists(pathCSVFile) == true)
                    {
                        pathCSVFile = pathFolderForResults + "ResultsByDim" + counterCSVFile++ + ".csv";
                    }
                    File.AppendAllText(pathCSVFile, contentCSVFile);
                }

                contentCSVFile = "";
            }








            #region serializing results to files

            string jsonString;
            string fileName;



            //Saving stats by dimensions to disk
            try
            {
                jsonString = JsonSerializer.Serialize(ListStatsForCSV);

                int counter = 1;
                fileName = pathFolderForResults + "ResByDimSerailized" + counter + ".txt";
                while (File.Exists(fileName) == true)
                {
                    fileName = pathFolderForResults + "ResByDimSerailized" + counter++ + ".txt";
                }

                File.WriteAllText(fileName, jsonString);
            }
            catch (Exception)
            {
                Console.WriteLine("Problem serializing the results 'ListStatsForCSV' ");
            }



            try
            {
                int counter = 1;

                //Serializing and saving the results of all tests to disk
                jsonString = JsonSerializer.Serialize(GlobalResults);

                fileName = pathFolderForResults + "GlobalResults_Serialized" + counter + ".txt";
                while (File.Exists(fileName) == true)
                {
                    fileName = pathFolderForResults + "GlobalResults_Serialized" + counter++ + ".txt";
                }

                File.AppendAllText(fileName, jsonString);
            }
            catch (Exception)
            {
                Console.WriteLine("Problem serializing the results 'pathFolderForResults' ");
            }




            #endregion


            Console.ReadKey();
            return;





        }



        private static List<IBenchmarkFunction> LoadBenchmarkFunctions(List<string> benchmarkNameToIgnoreList, List<string> benchmarkFunctionNameToIncludeList)
        {
            List<IBenchmarkFunction> usedBenchmarkFunctionList = new();
            List<string> AvailableBenchmarkFunctionsNamesList = new List<string>();
            Assembly ass;

            ass = System.Reflection.Assembly.GetEntryAssembly();
            foreach (System.Reflection.TypeInfo ti in ass.DefinedTypes)
            {
                if (ti.ImplementedInterfaces.Contains(typeof(IBenchmarkFunction)))
                {
                    if (benchmarkNameToIgnoreList.Where(x => ti.FullName.Contains(x) == true).Count() == 0)
                    {
                        if (benchmarkFunctionNameToIncludeList.Count == 0 || benchmarkFunctionNameToIncludeList.Any(x => ti.FullName.Contains(x) == true) == true)
                        {
                            AvailableBenchmarkFunctionsNamesList.Add(ti.FullName);
                        }
                    }
                }
            }

            AvailableBenchmarkFunctionsNamesList.Sort();



            foreach (string benchmarkFunctionItem in AvailableBenchmarkFunctionsNamesList)
            {
                object? createdInstance = null;
                createdInstance = ass.CreateInstance(benchmarkFunctionItem);

                if (createdInstance != null)
                {
                    usedBenchmarkFunctionList.Add((IBenchmarkFunction)createdInstance);
                }
            }

            return usedBenchmarkFunctionList;
        }

        private static List<Tuple<string, List<double>>> ComputeAndDisplayStats(List<GlobalBatchResultModel> GlobalResults, List<string> algoToIgnore, List<Tuple<MHOptimizationResult, StatsToComputeType>> StatsToComputeList)
        {
            List<Tuple<string, List<double>>> StatsByMHAlgoAndBenchFunc = new();



            StatsByMHAlgoAndBenchFunc = DiversExtendedProperties.ComputeStat(GlobalResults, StatsToComputeList, true, algoToIgnore, GroupByType.BenchmarkFunction, OrderingType.Ascending);


            Console.WriteLine(DiversExtendedProperties.FormatStatsResults(StatsByMHAlgoAndBenchFunc, StatsToComputeList));

            return StatsByMHAlgoAndBenchFunc;
        }



        private List<OptimizationParameter> DoOptimizationProcessConfiguration()
        {
            //Constructing the differents configuration
            List<OptimizationParameter> MHConf = new List<OptimizationParameter>();


            OptimizationParameter populationSizeParameter = new OptimizationParameter(Divers.MHAlgoParameters.PopulationSize, 40, true);
            OptimizationParameter stoppingCriteriaTypeParameter = new OptimizationParameter(Divers.MHAlgoParameters.StoppingCriteriaType, StoppingCriteriaType.MaximalNumberOfIteration);
            OptimizationParameter maxFunctionEvaluationNumberParameter = new OptimizationParameter(Divers.MHAlgoParameters.MaxFunctionEvaluationNumber, 200000);
            OptimizationParameter maxItertaionNumberParameter = new OptimizationParameter(Divers.MHAlgoParameters.MaxItertaionNumber, 1500, true);
            OptimizationParameter ProblemDimensionParameter = new OptimizationParameter(Divers.MHAlgoParameters.ProblemDimension, 30, true);
            OptimizationParameter OptimizationTypeParameter = new OptimizationParameter(Divers.MHAlgoParameters.OptimizationType, OptimizationProblemType.Minimization);
            OptimizationParameter PopulationInitilizationParameter = new OptimizationParameter(Divers.MHAlgoParameters.PopulationInitilization, PopulationInitilizationType.Random);
            OptimizationParameter ABC_LimitValueParameter = new OptimizationParameter(Divers.MHAlgoParameters.ABC_LimitValue, 100);
            OptimizationParameter MABC_ModificationRateParameter = new OptimizationParameter(Divers.MHAlgoParameters.MABC_ModificationRate, 0.4);
            OptimizationParameter MABC_UseScalingFactorParameter = new OptimizationParameter(Divers.MHAlgoParameters.MABC_UseScalingFactor, true);
            OptimizationParameter MABC_LimitValuerParameter = new OptimizationParameter(Divers.MHAlgoParameters.MABC_LimitValue, 200);
            OptimizationParameter FunctionValueSigmaToleranceParameter = new OptimizationParameter(Divers.MHAlgoParameters.FunctionValueSigmaTolerance, 1e-100);
            OptimizationParameter StopOptimizationWhenOptimumIsReachedParameter = new OptimizationParameter(Divers.MHAlgoParameters.StopOptimizationWhenOptimumIsReached, true);
            OptimizationParameter AEEABC_NumberOfIterationsToTuneParametersParameter = new OptimizationParameter(Divers.MHAlgoParameters.AEEABC_NumberOfIterationsToTuneParameters, 3);
            OptimizationParameter AEEABC_TuneNumberOfDimensionUsingGBestParameter = new OptimizationParameter(Divers.MHAlgoParameters.AEEABC_TuneNumberOfDimensionUsingGBest, true);
            OptimizationParameter AEEABC_InitialNumberOfDimensionUsingGBestRatioParameter = new OptimizationParameter(Divers.MHAlgoParameters.AEEABC_InitialNumberOfDimensionUsingGBestRatio, 0);
            OptimizationParameter AEEABC_TuneCoeff3ValueParameter = new OptimizationParameter(Divers.MHAlgoParameters.AEEABC_TuneCoeff3Value, true);
            OptimizationParameter AEEABC_Coeff3InitialValueParameter = new OptimizationParameter(Divers.MHAlgoParameters.AEEABC_Coeff3InitialValue, 0d);
            OptimizationParameter AEEABC_Coeff3ValueChangeParameter = new OptimizationParameter(Divers.MHAlgoParameters.AEEABC_Coeff3ValueChange, 0d);
            OptimizationParameter scoutGenerationParameter = new OptimizationParameter(Divers.MHAlgoParameters.ScoutGeneration, ScoutGenerationType.Random);
            OptimizationParameter ABC_ProbabilityEquationTypeParameter = new OptimizationParameter(Divers.MHAlgoParameters.ABC_ProbabilityEquationType, ABC_ProbabilityEquationType.Original);
            OptimizationParameter AEEABC_TuneScoutGenerationTypeParameter = new OptimizationParameter(Divers.MHAlgoParameters.AEEABC_TuneScoutGenerationType, true);
            OptimizationParameter AEEABC_TuneProbabilityEquationTypeParameter = new OptimizationParameter(Divers.MHAlgoParameters.AEEABC_TuneProbabilityEquationType, true);




            MHConf.Add(populationSizeParameter);
            MHConf.Add(stoppingCriteriaTypeParameter);
            MHConf.Add(maxFunctionEvaluationNumberParameter);
            MHConf.Add(maxItertaionNumberParameter);
            MHConf.Add(ProblemDimensionParameter);
            MHConf.Add(OptimizationTypeParameter);
            MHConf.Add(PopulationInitilizationParameter);
            MHConf.Add(ABC_LimitValueParameter);
            MHConf.Add(MABC_ModificationRateParameter);
            MHConf.Add(MABC_UseScalingFactorParameter);
            MHConf.Add(MABC_LimitValuerParameter);
            MHConf.Add(FunctionValueSigmaToleranceParameter);
            MHConf.Add(StopOptimizationWhenOptimumIsReachedParameter);
            MHConf.Add(AEEABC_NumberOfIterationsToTuneParametersParameter);
            MHConf.Add(AEEABC_TuneNumberOfDimensionUsingGBestParameter);
            MHConf.Add(AEEABC_InitialNumberOfDimensionUsingGBestRatioParameter);
            MHConf.Add(AEEABC_TuneCoeff3ValueParameter);
            MHConf.Add(AEEABC_Coeff3InitialValueParameter);
            MHConf.Add(AEEABC_Coeff3ValueChangeParameter);
            MHConf.Add(scoutGenerationParameter);
            MHConf.Add(ABC_ProbabilityEquationTypeParameter);
            MHConf.Add(AEEABC_TuneScoutGenerationTypeParameter);
            MHConf.Add(AEEABC_TuneProbabilityEquationTypeParameter);


            return MHConf;
        }

        private List<GlobalBatchResultModel>? StartOptimizationProcess(List<IMHAlgorithm> metaheuristicAlgorithmList, List<IBenchmarkFunction> usedBenchmarkFunctionList, int NumberRepetition, Random randomGenerator, bool RunOnlyOneProcess = false)
        {
            List<GlobalBatchResultModel>? GlobalResults = new List<GlobalBatchResultModel>();
            List<int> randomGeneratedIntegerList;
            Assembly ass = System.Reflection.Assembly.GetEntryAssembly();
            object resultLock = new object();
            int numberOfCompltedOptimizationProcesses = 0;
            bool processStoppedPrematurly = false;
            string processStoppedPrematurlyErrorMessage = "";
            GlobalBatchResultModel currentResul = new GlobalBatchResultModel();

            //COmputing the number of optimization processes to run
            int numberTotalOptimizationProcessesToRun = metaheuristicAlgorithmList.Count * usedBenchmarkFunctionList.Count * NumberRepetition;

            foreach (var MHAlgoItem in metaheuristicAlgorithmList)
            {//Loop for all MH algorithm to be tested
                foreach (var BenchmarkFuntionItem in usedBenchmarkFunctionList)
                {//Loop through all benchmark functions
                    string? benchmarkFunctionTypeName = BenchmarkFuntionItem.GetType().FullName;

                    //Call this method to prevent computerfrom sleeping during execution
                    NativeMethods.SetThreadExecutionState(EXECUTION_STATE.ES_SYSTEM_REQUIRED);

                    ParallelOptions parallelOptions = new ParallelOptions();
                    if (RunOnlyOneProcess == true)
                    {
                        parallelOptions.MaxDegreeOfParallelism = 1;
                    }
                    randomGenerator = new Random();

                    //Generating random numbers to be used as seed for the optimization algorithms
                    int randomIntValue;
                    randomGeneratedIntegerList = new List<int>();
                    for (int i = 0; i < NumberRepetition; i++)
                    {
                        randomGeneratedIntegerList.Add(randomGenerator.Next());
                    }

                    ParallelLoopResult resultLoopStatus = Parallel.For(0, NumberRepetition, parallelOptions, (RepetitionID, state) =>
                    //for (int RepetitionID = 0; RepetitionID < NumberRepetition; RepetitionID++)
                    {
                        var watch = new System.Diagnostics.Stopwatch();
                        watch.Restart();

                        //Repeat the same scenario 
                        List<OptimizationResultModel> resultsData = new();

                        IBenchmarkFunction localBenchmarkFunction;
                        object? createdInstance;
                        int tempInt;

                        try
                        {
                            createdInstance = ass.CreateInstance(benchmarkFunctionTypeName);
                        }
                        catch (Exception)
                        {
                            createdInstance = null;
                        }

                        if (createdInstance != null)
                        {
                            localBenchmarkFunction = (IBenchmarkFunction)createdInstance;
                            localBenchmarkFunction.ParentInstanceID = BenchmarkFuntionItem.ParentInstanceID;

                            lock (resultLock)
                            {
                                randomIntValue = randomGeneratedIntegerList[RepetitionID];
                            }

                            if (numberOfCompltedOptimizationProcesses == 1600)
                            {
                                tempInt = 2;
                            }


                            try
                            {
                                MHAlgoItem.ComputeOptimum(localBenchmarkFunction, resultsData, randomIntValue);
                            }
                            catch (Exception ex)
                            {
                                processStoppedPrematurly = true;
                                processStoppedPrematurlyErrorMessage = ex.ToString();
                                state.Stop();
                            }
                            Interlocked.Add(ref numberOfCompltedOptimizationProcesses, 1);
                            currentResul = new GlobalBatchResultModel(MHAlgoItem, localBenchmarkFunction, RepetitionID, resultsData);



                            if (processStoppedPrematurly == false)
                                lock (resultLock)
                                {
                                    #region Preparing status to be printed on screen
                                    string currentStatusText = "";
                                    StoppingCriteriaType stoppingCriteriaType = (StoppingCriteriaType)MHAlgoItem.OptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.StoppingCriteriaType).First().Value;
                                    int currentUsedDimension;
                                    int benchmarkFunctionMaxDimension = localBenchmarkFunction.MaxProblemDimension;
                                    int benchmarkFunctionMinDimension = localBenchmarkFunction.MinProblemDimension;
                                    int currentRequiredDimension = (int)MHAlgoItem.OptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.ProblemDimension).First().Value;

                                    if (currentRequiredDimension > benchmarkFunctionMaxDimension)
                                    {
                                        currentUsedDimension = benchmarkFunctionMaxDimension;
                                    }
                                    else if (currentRequiredDimension < benchmarkFunctionMinDimension)
                                    {
                                        currentUsedDimension = benchmarkFunctionMinDimension;
                                    }
                                    else
                                    {
                                        currentUsedDimension = currentRequiredDimension;
                                    }

                                    currentStatusText += MHAlgoItem.Name.ShortenNameString();
                                    currentStatusText += $"({MHAlgoItem.Description.ShortenNameString()})||";
                                    currentStatusText += localBenchmarkFunction.Name.ShortenNameString() + "||";
                                    //currentStatusText += "Pop : " + (int)MHAlgoItem.OptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.PopulationSize).First().Value;
                                    //if (stoppingCriteriaType == StoppingCriteriaType.MaximalNumberOfIteration)
                                    //{
                                    //    currentStatusText += "||MaxIter : " + (int)MHAlgoItem.OptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.MaxItertaionNumber).First().Value;
                                    //}
                                    //else if (stoppingCriteriaType == StoppingCriteriaType.MaximalNumberOfFunctionEvaluation)
                                    //{
                                    //    currentStatusText += "||MaxFuncEval : " + (int)MHAlgoItem.OptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.MaxFunctionEvaluationNumber).First().Value;
                                    //}
                                    currentStatusText += "||Dim : " + currentUsedDimension.ToString();

                                    currentStatusText += "||" + (RepetitionID + 1).ToString("D" + NumberRepetition.ToString().Length) + "/" + NumberRepetition.ToString("D" + NumberRepetition.ToString().Length);
                                    currentStatusText += "||FinalIterNum : " + resultsData.First(x => x.Name == MHOptimizationResult.NumberOfTotalIteration).Value;
                                    currentStatusText += "||FinalNbreFuncEval : " + resultsData.First(x => x.Name == MHOptimizationResult.NumberOfFunctionEvaluation).Value;
                                    currentStatusText += "||Time : " + resultsData.First(x => x.Name == MHOptimizationResult.ExecutionTime).Value;
                                    currentStatusText += "||OptValue : " + resultsData.First(x => x.Name == MHOptimizationResult.OptimalFunctionValue).Value;
                                    currentStatusText += "||Progress: " + numberOfCompltedOptimizationProcesses.ToString("D" + numberTotalOptimizationProcessesToRun.ToString().Length) + "/" + numberTotalOptimizationProcessesToRun.ToString("D" + numberTotalOptimizationProcessesToRun.ToString().Length);
                                    #endregion

                                    GlobalResults.Add(currentResul);
                                    Console.WriteLine(currentStatusText);
                                }
                        }

                    });

                    if (processStoppedPrematurly == true)
                    {
                        break;
                        // GlobalResults.Clear();
                        Console.WriteLine("A problem has been detected." + Environment.NewLine + "Optimization process has been halted. Please correct the issue first");
                        Console.WriteLine(processStoppedPrematurlyErrorMessage);
                        return null;
                    }
                }
                if (processStoppedPrematurly == true)
                {
                    break;
                    // GlobalResults.Clear();
                    Console.WriteLine("A problem has been detected." + Environment.NewLine + "Optimization process has been halted. Please correct the issue first");
                    Console.WriteLine(processStoppedPrematurlyErrorMessage);
                    return null;
                }
            }


            if (processStoppedPrematurly == true)
            {
                //GlobalResults.Clear();
                Console.WriteLine("A problem has been detected." + Environment.NewLine + "Optimization process has been halted. Please correct the issue first");
                Console.WriteLine(processStoppedPrematurlyErrorMessage);
                return null;
            }

            return GlobalResults;
        }




    }
}


