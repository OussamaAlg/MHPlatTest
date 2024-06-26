
using MathNet.Numerics.Financial;
using MHPlatTest.Divers;
using MHPlatTest.Interfaces;
using MHPlatTest.Models;
using MHPlatTest.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MHPlatTest.Algorithms
{
    /// <summary> 
    /// Adapitve ABC algorithm

    /// Adaptive number of optimization parameters to be updated
    /// Tuning of the 'C' constant within the update equation
    /// Tuning of the probability equation
    /// Tuning of the scout bee generation strategy
    /// </summary>
    class AdaABC : IMHAlgorithm
    {

        /// <summary>
        /// Create a new instance of the metaheuristic optimization algorithm
        /// </summary>
        /// <param name="optimizationConfiguration">the list of optimization configuration to be uapplied to the Algo</param>
        public AdaABC(List<OptimizationParameter> optimizationConfiguration, string description, int instanceID)
        {
            MakePersonalOptimizationConfigurationListCopy(optimizationConfiguration, description, instanceID);
        }

        public AdaABC()
        {
            //Generate unique identifier for current instance
            Random random = new Random();
            InstanceID = random.Next();
        }

        /// <summary>
        /// The name of the current metaheuristic optimization algorithm
        /// </summary>
        public string Name { get; set; } = "AdaABC";



        /// <summary>
        /// description of the current optimization algorithm
        /// when no decription is available, returns the 'InstanceID'
        /// </summary>
        private string _Description = "";

        public string Description
        {
            get
            {
                if (_Description == "")
                {
                    return InstanceID.ToString();
                }
                return _Description;
            }
            set { _Description = value; }
        }


        /// <summary>
        /// Contains an unique idenifier used to differentiate 
        /// between created instances of the same type
        /// </summary>
        public int InstanceID { get; set; }

        /// <summary>
        /// Contains the configuration to be applied with current optimization algorithm
        /// </summary>
        public List<OptimizationParameter> OptimizationConfiguration { get; set; } = new List<OptimizationParameter>();


        //Lock object used to thread safe the optimization result array
        object AccessSharedObjectLock = new object();


        /// <summary>
        /// Start the ABC algorithm with the specified configuration and returns the optimal value found
        /// </summary>
        /// <param name="threadSafeMethodArgs"></param>
        /// 
        public void ComputeOptimum(IBenchmarkFunction benchmarkFunction, List<OptimizationResultModel> resultsData, int randomSeed)
        {
            //Check to see whether the list of optimization configuration has been loaded
            //throw an exception if the list is empty
            if (OptimizationConfiguration.Count == 0)
            {
                throw new ArgumentNullException("optimizationConfiguration", "Please provide the optimization configuration parameters when selecting the metaheuristic optimization algorithm");
            }

            #region Making the method thread safe by making local variables for shared global objects
            //Making a distinct personal copy of the parameter list in this method to
            //make all variables local to this method in order to make it thread-safe
            List<OptimizationParameter> localOptimizationConfiguration = new();
            OptimizationParameter parameter;
            object? createdInstance;

            lock (AccessSharedObjectLock)
            {
                foreach (var ParameterItem in OptimizationConfiguration)
                {
                    parameter = new OptimizationParameter(ParameterItem.Name, ParameterItem.Value, ParameterItem.IsEssentialInfo);
                    localOptimizationConfiguration.Add(parameter);
                }

            }
            #endregion


            var watch = new System.Diagnostics.Stopwatch();
            watch.Restart();

            #region Reading and parsing algorithm parameters

            //Population Size
            int populationSize;
            try
            {
                populationSize = (int)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.PopulationSize).First().Value;
            }
            catch (Exception)
            {
                throw new Exception("Please select the population size !");
            }


            //Stopping Criteria
            StoppingCriteriaType stoppingCriteria;
            int maxItertaionNumber;
            int maxFunctionEvaluationNumber;
            double FunctionValueMinimumEnhancementThreshold;

            try
            {
                stoppingCriteria = (StoppingCriteriaType)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.StoppingCriteriaType).First().Value;
            }
            catch (Exception)
            {
                throw new Exception("Please select the stopping criteria type !");
            }

            switch (stoppingCriteria)
            {
                case StoppingCriteriaType.MaximalNumberOfIteration:
                    maxFunctionEvaluationNumber = int.MaxValue;
                    maxItertaionNumber = (int)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.MaxItertaionNumber).First().Value;
                    FunctionValueMinimumEnhancementThreshold = 0;
                    break;
                case StoppingCriteriaType.MaximalNumberOfFunctionEvaluation:
                    maxFunctionEvaluationNumber = (int)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.MaxFunctionEvaluationNumber).First().Value;
                    maxItertaionNumber = (int)Math.Ceiling((double)maxFunctionEvaluationNumber / (double)populationSize);
                    FunctionValueMinimumEnhancementThreshold = 0;
                    break;
                case StoppingCriteriaType.FunctionValueTolerance:
                    maxItertaionNumber = 1000000;
                    maxFunctionEvaluationNumber = int.MaxValue;
                    FunctionValueMinimumEnhancementThreshold = (double)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.FunctionValueMinimumEnhancementThreshold).First().Value;
                    break;
                default:
                    throw new Exception("Please select the stopping criteria type !");
            }


            //Problem Dimension
            int ProblemDimension;
            try
            {
                ProblemDimension = (int)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.ProblemDimension).First().Value;
            }
            catch (Exception)
            {
                throw new Exception("Please select the problem dimension !");
            }

            //Limit the dimension if its outside the benchmar function dimension limit
            if (benchmarkFunction.MaxProblemDimension < ProblemDimension)
            {
                ProblemDimension = benchmarkFunction.MaxProblemDimension;
            }

            if (benchmarkFunction.MinProblemDimension > ProblemDimension)
            {
                ProblemDimension = benchmarkFunction.MinProblemDimension;
            }


            //Optimization Type (Maximization / minimisation)
            OptimizationProblemType optimizationType;

            try
            {
                optimizationType = (OptimizationProblemType)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.OptimizationType).First().Value;
            }
            catch (Exception)
            {
                throw new Exception("Please select the optimization type (maximization/minimization) !");
            }


            // Population Initialization Scheme
            PopulationInitilizationType populationInitilizationScheme;

            try
            {
                populationInitilizationScheme = (PopulationInitilizationType)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.PopulationInitilization).First().Value;
            }
            catch (Exception)
            {
                throw new Exception("Please select the Population Initialization Scheme !");
            }


            // Scout generation Scheme
            ScoutGenerationType scoutGenerationScheme;

            try
            {
                scoutGenerationScheme = (ScoutGenerationType)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.ScoutGeneration).First().Value;
            }
            catch (Exception ex)
            {
                throw new Exception("Please select the scout generation Scheme !");
            }


            //ABC limit value
            int limitValue;
            try
            {
                limitValue = (int)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.ABC_LimitValue).First().Value;
            }
            catch (Exception)
            {
                throw new Exception("Please select the limit parameter value !");
            }




            //FunctionValueSigmaTolerance Parameter value
            double FunctionValueSigmaTolerance = double.MinValue;

            try
            {
                FunctionValueSigmaTolerance = (double)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.FunctionValueSigmaTolerance).First().Value;
            }
            catch (Exception)
            {
            }


            //ShiftObjectiveFunctionOptimumValueToZero Parameter value
            bool ShiftObjectiveFunctionOptimumValueToZero = false;

            try
            {
                ShiftObjectiveFunctionOptimumValueToZero = (bool)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.ShiftObjectiveFunctionOptimumValueToZero).First().Value;
            }
            catch (Exception)
            {
            }


            //StopOptimizationWhenOptimumIsReached Parameter value
            bool StopOptimizationWhenOptimumIsReached = false;

            try
            {
                StopOptimizationWhenOptimumIsReached = (bool)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.StopOptimizationWhenOptimumIsReached).First().Value;
            }
            catch (Exception)
            {
            }


            //AEEABC_TuneNumberOfDimensionUsingGBest Parameter value
            bool AEEABC_TuneNumberOfDimensionUsingGBest = false;

            try
            {
                AEEABC_TuneNumberOfDimensionUsingGBest = (bool)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.AEEABC_TuneNumberOfDimensionUsingGBest).First().Value;
            }
            catch (Exception)
            {
            }


            //AEEABC_NumberOfIterationsToTuneParameters Parameter value
            int AEEABC_NumberOfIterationsToTuneParameters = 0;

            try
            {
                AEEABC_NumberOfIterationsToTuneParameters = (int)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.AEEABC_NumberOfIterationsToTuneParameters).First().Value;
            }
            catch (Exception)
            {
                throw new Exception("Please specify the Number Of Iterations required To Tune Parameters values !");
            }



            //AEEABC_InitialNumberOfDimensionUsingGBest Parameter value
            double AEEABC_InitialNumberOfDimensionUsingGBestRatio = 0;

            try
            {
                AEEABC_InitialNumberOfDimensionUsingGBestRatio = (double)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.AEEABC_InitialNumberOfDimensionUsingGBestRatio).First().Value;
            }
            catch (Exception)
            {
                AEEABC_InitialNumberOfDimensionUsingGBestRatio = 0;
            }


            //AEEABC_TuneCoeff3Value Parameter value
            bool AEEABC_TuneCoeff3Value = false;

            try
            {
                AEEABC_TuneCoeff3Value = (bool)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.AEEABC_TuneCoeff3Value).First().Value;
            }
            catch (Exception)
            {
            }


            //AEEABC_Coeff1InitialValue Parameter value
            double AEEABC_Coeff3InitialValue = 1;

            try
            {
                AEEABC_Coeff3InitialValue = (double)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.AEEABC_Coeff3InitialValue).First().Value;
            }
            catch (Exception)
            {
                AEEABC_Coeff3InitialValue = 1;
            }

            //AEEABC_Coeff1ValueChange Parameter value
            double AEEABC_Coeff3ValueChange = 0;

            try
            {
                AEEABC_Coeff3ValueChange = (double)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.AEEABC_Coeff3ValueChange).First().Value;
            }
            catch (Exception)
            {
                AEEABC_Coeff3ValueChange = 0;
            }


            //ABC_ProbabilityEquationType Parameter value
            ABC_ProbabilityEquationType ABC_ProbabilityEquationType = ABC_ProbabilityEquationType.Original;

            try
            {
                ABC_ProbabilityEquationType = (ABC_ProbabilityEquationType)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.ABC_ProbabilityEquationType).First().Value;
            }
            catch (Exception)
            {
                ABC_ProbabilityEquationType = ABC_ProbabilityEquationType.Original;
            }




            //AEEABC_TuneScoutGenerationTypeParameters Parameter value
            bool AEEABC_TuneScoutGenerationTypeParameters = false;

            try
            {
                AEEABC_TuneScoutGenerationTypeParameters = (bool)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.AEEABC_TuneScoutGenerationType).First().Value;
            }
            catch (Exception)
            {
            }




            //AEEABC_TuneProbabilityEquationTypeParameter Parameter value
            bool AEEABC_TuneProbabilityEquationTypeParameter = false;

            try
            {
                AEEABC_TuneProbabilityEquationTypeParameter = (bool)localOptimizationConfiguration.Where(x => x.Name == MHAlgoParameters.AEEABC_TuneProbabilityEquationType).First().Value;
            }
            catch (Exception)
            {
            }



            #endregion




            #region Declataion of variables, objects and arrays
            int nbrEmployed = (int)Math.Floor(populationSize / 2.0d);
            int nbrOnlooker = (int)Math.Ceiling(populationSize / 2.0d);
            double[] searchSpaceMinValue, searchSpaceMaxValue, searchSpaceRangeValue;
            double[,] populationArray = new double[populationSize, ProblemDimension];
            double[] valueObjectiveFunctionArray = new double[populationSize];
            double[] fitnessArray = new double[populationSize];
            double[] wheightingProbabilityForScoutGenerationArray = new double[nbrEmployed];
            int[] limitValueForFoodSourcesArray = new int[populationSize];
            double globalBestFitness = 0;
            double globalBestValueObjectiveFunction;
            double[] globalBestPosition = new double[ProblemDimension];
            double c_Constant = 1.5;
            List<int> ListFirstFoodSourceID = new();
            int firstFoodSourceID;
            int currentNumberofunctionEvaluation = 0;
            List<double> bestObjectiveFunctionEvaluationData = new List<double>();


            //Variables used to store data about total/Successfull mutation count
            int TotalMutationCount = 0;
            int SuccessfullMutationCount = 0;
            List<int> TotalMutationCountList = new List<int>();
            List<int> SuccessfullMutationCountList = new List<int>();
            int[] foodSourceExploitationCountArray = new int[populationSize];


            //Variables used to tune AEEABC parameters
            List<double> successfullMutationsRates = new List<double>();
            List<AEEABCtunningParametersNames> NameAvailableParametersToTuneList = new List<AEEABCtunningParametersNames>();
            List<OrderingType> tunedParameterDirectionList = new() { OrderingType.Ascending, OrderingType.Ascending, OrderingType.Ascending, OrderingType.Ascending, OrderingType.Ascending, OrderingType.Ascending, OrderingType.Ascending, OrderingType.Ascending, OrderingType.Ascending, OrderingType.Ascending, OrderingType.Ascending, OrderingType.Ascending, OrderingType.Ascending, OrderingType.Ascending, OrderingType.Ascending, OrderingType.Ascending };
            double currentCycleSuccessfullMutationRate = 0;
            double previousCycleSuccessfullMutationRate = 0;
            bool currentSuccessMutaRateBetter = false;
            int AEEABC_NumberOfDimensionUsingGBest = (int)Math.Ceiling(AEEABC_InitialNumberOfDimensionUsingGBestRatio * (int)ProblemDimension);
            int AEEABC_NumberDimensionToUpdate = 0;


            if (AEEABC_TuneNumberOfDimensionUsingGBest == true)
            {
                AEEABC_NumberOfDimensionUsingGBest = Math.Max((int)Math.Floor(AEEABC_InitialNumberOfDimensionUsingGBestRatio * ProblemDimension), 1);
                AEEABC_NumberDimensionToUpdate = 0;
            }
            else
            {
                AEEABC_NumberOfDimensionUsingGBest = 1;
            }
            if (AEEABC_NumberOfDimensionUsingGBest < 1 && AEEABC_TuneNumberOfDimensionUsingGBest) AEEABC_NumberOfDimensionUsingGBest = 1;
            if (AEEABC_NumberOfDimensionUsingGBest > ProblemDimension) AEEABC_NumberOfDimensionUsingGBest = ProblemDimension;


            double AEEABC_PhiRangeValue = 1;
            int indexCurrentParameterToTune = -1;
            int numberOfAllTunedParameter = 0;  //The number of the parameter being tuned
            List<int> IndexAllParametersToTuneCurrentIterationList = new List<int>();
            List<int> nbreDimByIteratList = new List<int>();
            List<int> nbreGBestDimByIteratList = new List<int>();
            bool changeindexCurrentParameterToTune = false;
            double originalParameterValueBeforeUpdate = 0;


            //Tuning Beta parameter in update equation
            double AEEABC_Coeff1Value = 1;
            double AEEABC_Coeff2Value = 1;
            double AEEABC_Coeff3Value = 1;



            //Computing the current optimal value (depend on 'ShiftObjectiveFunctionOptimumValueToZero' )
            double currentObjectiveFunctionOptimum;
            if (ShiftObjectiveFunctionOptimumValueToZero == true)
            {
                currentObjectiveFunctionOptimum = 0;
            }
            else
            {
                currentObjectiveFunctionOptimum = benchmarkFunction.OptimalFunctionValue(ProblemDimension);
            }
            #endregion



            #region Algorithm Initialization
            //If the same search space limit is for all dimensions
            //extend this limit for the other dimension starting
            //from the first if only the first dimension limit is set
            if (benchmarkFunction.SearchSpaceMinValue.Length < ProblemDimension)
            {
                double[] currentSearchSpaceMinValue = benchmarkFunction.SearchSpaceMinValue;
                benchmarkFunction.SearchSpaceMinValue = new double[ProblemDimension];

                for (int i = 0; i < ProblemDimension; i++)
                {
                    if (i < currentSearchSpaceMinValue.Length)
                    {
                        benchmarkFunction.SearchSpaceMinValue[i] = currentSearchSpaceMinValue[i];
                    }
                    else
                    {
                        benchmarkFunction.SearchSpaceMinValue[i] = benchmarkFunction.SearchSpaceMinValue[i - 1];
                    }
                }
            }
            if (benchmarkFunction.SearchSpaceMaxValue.Length < ProblemDimension)
            {
                double[] currentSearchSpaceMaxValue = benchmarkFunction.SearchSpaceMaxValue;
                benchmarkFunction.SearchSpaceMaxValue = new double[ProblemDimension];

                for (int i = 0; i < ProblemDimension; i++)
                {
                    if (i < currentSearchSpaceMaxValue.Length)
                    {
                        benchmarkFunction.SearchSpaceMaxValue[i] = currentSearchSpaceMaxValue[i];
                    }
                    else
                    {
                        benchmarkFunction.SearchSpaceMaxValue[i] = benchmarkFunction.SearchSpaceMaxValue[i - 1];
                    }
                }
            }

            var rand = new Random(randomSeed);
            searchSpaceMinValue = benchmarkFunction.SearchSpaceMinValue;
            searchSpaceMaxValue = benchmarkFunction.SearchSpaceMaxValue;
            searchSpaceRangeValue = new double[searchSpaceMaxValue.Length];

            for (int i = 0; i < searchSpaceMaxValue.Length; i++)
            {
                searchSpaceRangeValue[i] = searchSpaceMaxValue[i] - searchSpaceMinValue[i];
            }

            globalBestFitness = double.MinValue;
            switch (optimizationType)
            {
                case OptimizationProblemType.Maximization:
                    globalBestValueObjectiveFunction = double.MinValue;
                    break;
                case OptimizationProblemType.Minimization:
                    globalBestValueObjectiveFunction = double.MaxValue;
                    break;
                default:
                    globalBestValueObjectiveFunction = double.MaxValue;
                    break;
            }


            //Fill the 'NameAvailableParametersToTuneList' with the desured list of parameter to be tuned
            if (AEEABC_TuneNumberOfDimensionUsingGBest == true) NameAvailableParametersToTuneList.Add(AEEABCtunningParametersNames.NumberOfDimensionUsingGBest);
            if (AEEABC_TuneCoeff3Value == true) NameAvailableParametersToTuneList.Add(AEEABCtunningParametersNames.Coeff3Value);
            if (AEEABC_TuneScoutGenerationTypeParameters == true) NameAvailableParametersToTuneList.Add(AEEABCtunningParametersNames.ScoutGenerationType);
            if (AEEABC_TuneProbabilityEquationTypeParameter == true) NameAvailableParametersToTuneList.Add(AEEABCtunningParametersNames.ProbabilityEquationType);

            numberOfAllTunedParameter = NameAvailableParametersToTuneList.Count;


            #endregion



            #region Population initialzation
            switch (populationInitilizationScheme)
            {
                case PopulationInitilizationType.Random:
                    for (int arrayLine = 0; arrayLine < nbrEmployed; arrayLine++)
                    {
                        for (int arrayColumn = 0; arrayColumn < ProblemDimension; arrayColumn++)
                        {
                            populationArray[arrayLine, arrayColumn] = searchSpaceMinValue[arrayColumn] + ((double)rand.NextDouble() * searchSpaceRangeValue[arrayColumn]);
                        }
                        limitValueForFoodSourcesArray[arrayLine] = 0;
                    }
                    break;
                default:
                    for (int arrayLine = 0; arrayLine < populationSize; arrayLine++)
                    {
                        for (int arrayColumn = 0; arrayColumn < ProblemDimension; arrayColumn++)
                        {
                            populationArray[arrayLine, arrayColumn] = searchSpaceMinValue[arrayColumn] + ((double)rand.NextDouble() * searchSpaceRangeValue[arrayColumn]);
                        }
                        limitValueForFoodSourcesArray[arrayLine] = 0;
                    }
                    break;
            }
            #endregion




            #region Computing the cost of the initial employed food sources
            for (int arrayLine = 0; arrayLine < nbrEmployed; arrayLine++)
            {
                double[] currentParticlePositionArray = new double[ProblemDimension];

                //Obtaining the position of current particle
                for (int arrayColumn = 0; arrayColumn < ProblemDimension; arrayColumn++)
                {
                    currentParticlePositionArray[arrayColumn] = populationArray[arrayLine, arrayColumn];
                }

                //Computing the cost of the current particle
                valueObjectiveFunctionArray[arrayLine] = benchmarkFunction.ComputeValue(currentParticlePositionArray, ref currentNumberofunctionEvaluation, ShiftObjectiveFunctionOptimumValueToZero);


                //Checking if the FunctionValueSigmaTolerance has been reached
                if (FunctionValueSigmaTolerance > Math.Abs(valueObjectiveFunctionArray[arrayLine]))
                {
                    valueObjectiveFunctionArray[arrayLine] = 0;
                }

                //Computing the fitness value
                fitnessArray[arrayLine] = valueObjectiveFunctionArray[arrayLine].ComputeFitness(optimizationType);


                //Updating the GlobalBest Position
                if (globalBestFitness < fitnessArray[arrayLine])
                {
                    for (int arrayColumn = 0; arrayColumn < ProblemDimension; arrayColumn++)
                    {
                        globalBestPosition[arrayColumn] = populationArray[arrayLine, arrayColumn];
                    }
                    globalBestValueObjectiveFunction = valueObjectiveFunctionArray[arrayLine];
                    globalBestFitness = fitnessArray[arrayLine];


                    //Check whether the optimization process should be
                    //stopped if the optimal value has been reached
                    if (StopOptimizationWhenOptimumIsReached == true && (Math.Abs(currentObjectiveFunctionOptimum - globalBestValueObjectiveFunction) < FunctionValueSigmaTolerance || Math.Abs(currentObjectiveFunctionOptimum - globalBestValueObjectiveFunction) < double.MinValue))
                    {
                        TotalMutationCountList.Add(0);
                        SuccessfullMutationCountList.Add(0);

                        //collect current best function eval list for post data analysis 
                        bestObjectiveFunctionEvaluationData.Add(globalBestValueObjectiveFunction);

                        PrepareResultData(resultsData, MHOptimizationResult.OptimalFunctionValue, globalBestValueObjectiveFunction);
                        PrepareResultData(resultsData, MHOptimizationResult.NumberOfFunctionEvaluation, currentNumberofunctionEvaluation);
                        PrepareResultData(resultsData, MHOptimizationResult.NumberOfTotalIteration, 0);
                        PrepareResultData(resultsData, MHOptimizationResult.OptimalPoint, globalBestPosition);
                        PrepareResultData(resultsData, MHOptimizationResult.ExecutionTime, watch.ElapsedMilliseconds);
                        PrepareResultData(resultsData, MHOptimizationResult.OptimumFound, true);
                        PrepareResultData(resultsData, MHOptimizationResult.TotalMutationCountData, 0);
                        PrepareResultData(resultsData, MHOptimizationResult.TotalSuccessfullMutationCountData, 0);
                        PrepareResultData(resultsData, MHOptimizationResult.ObjectiveFunctionEvaluationData, bestObjectiveFunctionEvaluationData);

                        return;
                    }
                }

                ////collect current best function eval list for post data analysis 
                //if (currentNumberofunctionEvaluation % 250000 == 0) bestObjectiveFunctionEvaluationData.Add(globalBestValueObjectiveFunction);

                //Check to see whether we have depleted the number of allowed function evaluation
                if (currentNumberofunctionEvaluation > maxFunctionEvaluationNumber)
                {
                    TotalMutationCountList.Add(0);
                    SuccessfullMutationCountList.Add(0);

                    //collect current best function eval list for post data analysis 
                    if (currentNumberofunctionEvaluation % 250000 != 0) bestObjectiveFunctionEvaluationData.Add(globalBestValueObjectiveFunction);

                    PrepareResultData(resultsData, MHOptimizationResult.OptimalFunctionValue, globalBestValueObjectiveFunction);
                    PrepareResultData(resultsData, MHOptimizationResult.NumberOfFunctionEvaluation, currentNumberofunctionEvaluation);
                    PrepareResultData(resultsData, MHOptimizationResult.NumberOfTotalIteration, 0);
                    PrepareResultData(resultsData, MHOptimizationResult.OptimalPoint, globalBestPosition);
                    PrepareResultData(resultsData, MHOptimizationResult.ExecutionTime, watch.ElapsedMilliseconds);
                    PrepareResultData(resultsData, MHOptimizationResult.OptimumFound, false);
                    PrepareResultData(resultsData, MHOptimizationResult.TotalMutationCountData, 0);
                    PrepareResultData(resultsData, MHOptimizationResult.TotalSuccessfullMutationCountData, 0);
                    PrepareResultData(resultsData, MHOptimizationResult.ObjectiveFunctionEvaluationData, bestObjectiveFunctionEvaluationData);

                    return;
                }
            }
            #endregion



            #region Iterative process
            int iterationNumber;
            //Choosing the all dimensions that need to be updated
            List<int> DimensionToUpdateList = new List<int>();
            List<int> DimensionToUpdateUsingGbestList = new List<int>();
            List<int> DimensionAlreadyTaken = new List<int>();
            //List<int> DimensionAlreadyTaken = new List<int>();


            for (iterationNumber = 0; iterationNumber < maxItertaionNumber; iterationNumber++)
            {

                #region Employed bee phase
                for (int arrayLine = 0; arrayLine < nbrEmployed; arrayLine++)
                {
                    //Obtaining the position of current particle
                    double[] currentParticlePositionArray = new double[ProblemDimension];

                    for (int arrayColumn = 0; arrayColumn < ProblemDimension; arrayColumn++)
                    {
                        currentParticlePositionArray[arrayColumn] = populationArray[arrayLine, arrayColumn];
                    }

                    //Updating the food source exploitation count 
                    foodSourceExploitationCountArray[arrayLine]++;


                    //Choosing the dimension that need to be updated
                    DimensionToUpdateList.Clear();
                    DimensionToUpdateUsingGbestList.Clear();
                    GenerateListForStandardDimandGbestDim(ProblemDimension - 1, rand, AEEABC_NumberDimensionToUpdate, DimensionToUpdateList, AEEABC_NumberOfDimensionUsingGBest, DimensionToUpdateUsingGbestList);


                    //Retrieving all dimensions to update
                    DimensionAlreadyTaken.Clear();
                    DimensionAlreadyTaken.AddRange(DimensionToUpdateList);
                    DimensionAlreadyTaken.AddRange(DimensionToUpdateUsingGbestList);


                    //Choosing the randomly selected food source used to update the current food source
                    //Consider as if the currentfood source is not included in the selection 'nbrEmployed-1'
                    ListFirstFoodSourceID.Clear();
                    for (int i = 0; i < nbrEmployed; i++)
                    {
                        if (i != arrayLine)
                        {
                            ListFirstFoodSourceID.Add(i);
                        }
                    }

                    //Updating the desired dimensions
                    foreach (var dimensionToUpdateItem in DimensionAlreadyTaken)
                    {
                        //Selecting the first food source
                        int foodSourceIndex = (int)Math.Floor(rand.NextDouble() * ListFirstFoodSourceID.Count);
                        firstFoodSourceID = ListFirstFoodSourceID[foodSourceIndex];

                        if (DimensionToUpdateUsingGbestList.Contains(dimensionToUpdateItem) == true)
                        {  //Current dimension is updated using GABC update equation
                            //Computing values for the update equation
                            double phiValue = (rand.NextDouble() * AEEABC_PhiRangeValue * 2) - AEEABC_PhiRangeValue;

                            double distanceBetweenFoodSources = populationArray[arrayLine, dimensionToUpdateItem] - populationArray[firstFoodSourceID, dimensionToUpdateItem];
                            double distancetoGlobalBestFoodSource = globalBestPosition[dimensionToUpdateItem] - populationArray[arrayLine, dimensionToUpdateItem];

                            // update the food source location
                            currentParticlePositionArray[dimensionToUpdateItem] = AEEABC_Coeff1Value * populationArray[arrayLine, dimensionToUpdateItem]
                                                                                + AEEABC_Coeff2Value * phiValue * distanceBetweenFoodSources
                                                                                + AEEABC_Coeff3Value * rand.NextDouble() * distancetoGlobalBestFoodSource;
                        }
                        else
                        { //Current dimension is updated using standard equation
                            //Computing values for the update equation
                            double phiValue = (rand.NextDouble() * AEEABC_PhiRangeValue * 2) - AEEABC_PhiRangeValue;
                            double distanceBetweenFoodSources = populationArray[arrayLine, dimensionToUpdateItem] - populationArray[firstFoodSourceID, dimensionToUpdateItem];

                            // update the food source location
                            currentParticlePositionArray[dimensionToUpdateItem] = populationArray[arrayLine, dimensionToUpdateItem]
                                                                                + phiValue * distanceBetweenFoodSources;
                        }


                        //check if newly food sources within the serach space boundary
                        if (currentParticlePositionArray[dimensionToUpdateItem] > benchmarkFunction.SearchSpaceMaxValue[dimensionToUpdateItem])
                        {
                            currentParticlePositionArray[dimensionToUpdateItem] = benchmarkFunction.SearchSpaceMaxValue[dimensionToUpdateItem];
                        }
                        if (currentParticlePositionArray[dimensionToUpdateItem] < benchmarkFunction.SearchSpaceMinValue[dimensionToUpdateItem])
                        {
                            currentParticlePositionArray[dimensionToUpdateItem] = benchmarkFunction.SearchSpaceMinValue[dimensionToUpdateItem];
                        }
                    }


                    //Computing the cost of the current particle
                    double currentParticleValueObjectiveFunction = benchmarkFunction.ComputeValue(currentParticlePositionArray, ref currentNumberofunctionEvaluation, ShiftObjectiveFunctionOptimumValueToZero);

                    //Checking if the FunctionValueSigmaTolerance has been reached
                    if (FunctionValueSigmaTolerance > Math.Abs(currentParticleValueObjectiveFunction))
                    {
                        currentParticleValueObjectiveFunction = 0;
                    }

                    //Computing the fitness
                    double currentParticleFitness = currentParticleValueObjectiveFunction.ComputeFitness(optimizationType);

                    TotalMutationCount++;

                    //Apply the greedy selection on newly generated food source
                    if (currentParticleFitness > fitnessArray[arrayLine])
                    {
                        //Newly generated food source is better
                        for (int arrayColumn = 0; arrayColumn < ProblemDimension; arrayColumn++)
                        {
                            populationArray[arrayLine, arrayColumn] = currentParticlePositionArray[arrayColumn];
                        }
                        valueObjectiveFunctionArray[arrayLine] = currentParticleValueObjectiveFunction;
                        fitnessArray[arrayLine] = currentParticleFitness;

                        limitValueForFoodSourcesArray[arrayLine] = 0;
                        SuccessfullMutationCount++;

                        //Updating the GlobalBest Position
                        if (globalBestFitness < currentParticleFitness)
                        {
                            for (int arrayColumn = 0; arrayColumn < ProblemDimension; arrayColumn++)
                            {
                                globalBestPosition[arrayColumn] = populationArray[arrayLine, arrayColumn];
                            }
                            globalBestFitness = currentParticleFitness;
                            globalBestValueObjectiveFunction = currentParticleValueObjectiveFunction;

                            //Check whether the optimization process should be
                            //stopped if the optimal value has been reached
                            if (StopOptimizationWhenOptimumIsReached == true && (Math.Abs(currentObjectiveFunctionOptimum - globalBestValueObjectiveFunction) < FunctionValueSigmaTolerance || Math.Abs(currentObjectiveFunctionOptimum - globalBestValueObjectiveFunction) < double.MinValue))
                            {
                                TotalMutationCountList.Add(TotalMutationCount);
                                SuccessfullMutationCountList.Add(SuccessfullMutationCount);

                                //collect current best function eval list for post data analysis 
                                bestObjectiveFunctionEvaluationData.Add(globalBestValueObjectiveFunction);

                                PrepareResultData(resultsData, MHOptimizationResult.OptimalFunctionValue, globalBestValueObjectiveFunction);
                                PrepareResultData(resultsData, MHOptimizationResult.NumberOfFunctionEvaluation, currentNumberofunctionEvaluation);
                                PrepareResultData(resultsData, MHOptimizationResult.NumberOfTotalIteration, iterationNumber);
                                PrepareResultData(resultsData, MHOptimizationResult.OptimalPoint, globalBestPosition);
                                PrepareResultData(resultsData, MHOptimizationResult.ExecutionTime, watch.ElapsedMilliseconds);
                                PrepareResultData(resultsData, MHOptimizationResult.OptimumFound, true);
                                PrepareResultData(resultsData, MHOptimizationResult.TotalMutationCountData, new List<int>());
                                PrepareResultData(resultsData, MHOptimizationResult.TotalSuccessfullMutationCountData, new List<int>());
                                PrepareResultData(resultsData, MHOptimizationResult.ObjectiveFunctionEvaluationData, bestObjectiveFunctionEvaluationData);

                                return;
                            }
                        }
                    }
                    else
                    {
                        //newly generated food source is not better
                        //increase the limit value counter for current food source
                        limitValueForFoodSourcesArray[arrayLine]++;
                    }

                    //Check to see whether we have depleted the number of allowed function evaluation
                    if ((currentNumberofunctionEvaluation > maxFunctionEvaluationNumber))// 
                    {
                        TotalMutationCountList.Add(TotalMutationCount);
                        SuccessfullMutationCountList.Add(SuccessfullMutationCount);

                        PrepareResultData(resultsData, MHOptimizationResult.OptimalFunctionValue, globalBestValueObjectiveFunction);
                        PrepareResultData(resultsData, MHOptimizationResult.NumberOfFunctionEvaluation, currentNumberofunctionEvaluation);
                        PrepareResultData(resultsData, MHOptimizationResult.NumberOfTotalIteration, iterationNumber);
                        PrepareResultData(resultsData, MHOptimizationResult.OptimalPoint, globalBestPosition);
                        PrepareResultData(resultsData, MHOptimizationResult.ExecutionTime, watch.ElapsedMilliseconds);
                        PrepareResultData(resultsData, MHOptimizationResult.OptimumFound, false);
                        PrepareResultData(resultsData, MHOptimizationResult.TotalMutationCountData, new List<int>());
                        PrepareResultData(resultsData, MHOptimizationResult.TotalSuccessfullMutationCountData, new List<int>());
                        PrepareResultData(resultsData, MHOptimizationResult.ObjectiveFunctionEvaluationData, bestObjectiveFunctionEvaluationData);

                        return;
                    }
                }
                #endregion



                #region Onlooker bee phase

                // Computing the probability of each food source to be selected by an onlooker
                double costArraySum = fitnessArray.Where((x, Index) => Index >= 0 && Index < nbrEmployed).Sum();
                double[] probabilityFirstPartArray = new double[nbrEmployed];
                double[] probabilityArray = new double[nbrEmployed];
                double[] inverseProbabilityFirstPartArray = new double[nbrEmployed];
                double inverseCostArraySum = 0;

                //Computing the first part of the new probability exploitation
                probabilityFirstPartArray[0] = fitnessArray[0] / costArraySum;

                for (int arrayLine = 1; arrayLine < nbrEmployed; arrayLine++)
                {
                    probabilityFirstPartArray[arrayLine] = probabilityFirstPartArray[arrayLine - 1] + fitnessArray[arrayLine] / costArraySum;
                }


                //Computing the inverse of the probability equation
                for (int arrayLine = 0; arrayLine < nbrEmployed; arrayLine++)
                {
                    inverseCostArraySum = inverseCostArraySum + (1 / fitnessArray[arrayLine]);
                }

                inverseProbabilityFirstPartArray[0] = (1 / fitnessArray[0]) / inverseCostArraySum;

                for (int arrayLine = 1; arrayLine < nbrEmployed; arrayLine++)
                {
                    inverseProbabilityFirstPartArray[arrayLine] = inverseProbabilityFirstPartArray[arrayLine - 1] + ((1 / fitnessArray[arrayLine]) / inverseCostArraySum);
                }

                //Computing the total probaility
                switch (ABC_ProbabilityEquationType)
                {
                    case ABC_ProbabilityEquationType.Original:
                        for (int arrayLine = 0; arrayLine < nbrEmployed; arrayLine++)
                        {
                            probabilityArray[arrayLine] = probabilityFirstPartArray[arrayLine];
                        }

                        break;
                    case ABC_ProbabilityEquationType.ComplementOriginal:
                        for (int arrayLine = 0; arrayLine < nbrEmployed; arrayLine++)
                        {
                            probabilityArray[arrayLine] = inverseProbabilityFirstPartArray[arrayLine];
                        }
                        break;
                    default:
                        break;
                }

                //Browse through all onlookers
                for (int arrayLine = 0; arrayLine < nbrOnlooker; arrayLine++)
                {
                    //1- choosing the  food source that the current onlooker will go to
                    double randomValueSmallerThan1;
                    int selectedFoodSourceID = 0;
                    randomValueSmallerThan1 = rand.NextDouble();

                    for (int foodSourceID = 0; foodSourceID < nbrEmployed; foodSourceID++)
                    {
                        if (probabilityArray[foodSourceID] > randomValueSmallerThan1)
                        {
                            selectedFoodSourceID = foodSourceID;
                            break;
                        }
                    }

                    //2- Obtaining the position of the selected food source
                    double[] currentParticlePositionArray = new double[ProblemDimension];

                    for (int arrayColumn = 0; arrayColumn < ProblemDimension; arrayColumn++)
                    {
                        currentParticlePositionArray[arrayColumn] = populationArray[selectedFoodSourceID, arrayColumn];
                    }

                    //Updating the food source exploitation count 
                    foodSourceExploitationCountArray[selectedFoodSourceID]++;


                    //Choosing the dimensions that need to be updated
                    DimensionToUpdateList.Clear();
                    DimensionToUpdateUsingGbestList.Clear();
                    GenerateListForStandardDimandGbestDim(ProblemDimension - 1, rand, AEEABC_NumberDimensionToUpdate, DimensionToUpdateList, AEEABC_NumberOfDimensionUsingGBest, DimensionToUpdateUsingGbestList);


                    //Retrieving all dimensions to update
                    DimensionAlreadyTaken.Clear();
                    DimensionAlreadyTaken.AddRange(DimensionToUpdateList);
                    DimensionAlreadyTaken.AddRange(DimensionToUpdateUsingGbestList);


                    //Choosing the randomly selected food source used to update the current food source
                    //Consider as if the currentfood source is not included in the selection 'nbrEmployed-1'
                    ListFirstFoodSourceID.Clear();
                    for (int i = 0; i < nbrEmployed; i++)
                    {
                        if (i != arrayLine)
                        {
                            ListFirstFoodSourceID.Add(i);
                        }
                    }

                    //Updating the desired dimensions
                    foreach (var dimensionToUpdateItem in DimensionAlreadyTaken)
                    {
                        //Selecting firstfood source
                        int foosSourceIndex = (int)Math.Floor(rand.NextDouble() * ListFirstFoodSourceID.Count);
                        firstFoodSourceID = ListFirstFoodSourceID[foosSourceIndex];

                        if (DimensionToUpdateUsingGbestList.Contains(dimensionToUpdateItem) == true)
                        {//Current dimension is updated using GABC update equation

                            //Computing values for the update equation
                            double phiValue = (rand.NextDouble() * AEEABC_PhiRangeValue * 2) - AEEABC_PhiRangeValue;
                            double kciValue;

                            kciValue = rand.NextDouble() * c_Constant;

                            double distanceBetweenFoodSources = populationArray[selectedFoodSourceID, dimensionToUpdateItem] - populationArray[firstFoodSourceID, dimensionToUpdateItem];
                            double distancetoGlobalBestFoodSource = globalBestPosition[dimensionToUpdateItem] - populationArray[selectedFoodSourceID, dimensionToUpdateItem];

                            // update the food source location
                            currentParticlePositionArray[dimensionToUpdateItem] = AEEABC_Coeff1Value * populationArray[selectedFoodSourceID, dimensionToUpdateItem]
                                                                                + AEEABC_Coeff2Value * phiValue * distanceBetweenFoodSources
                                                                                + AEEABC_Coeff3Value * rand.NextDouble() * distancetoGlobalBestFoodSource;
                        }
                        else
                        {//Current dimension is updated using standard equation
                            //Computing values for the update equation
                            double phiValue = (rand.NextDouble() * AEEABC_PhiRangeValue * 2) - AEEABC_PhiRangeValue;
                            double distanceBetweenFoodSources = populationArray[selectedFoodSourceID, dimensionToUpdateItem] - populationArray[firstFoodSourceID, dimensionToUpdateItem];

                            // update the food source location
                            currentParticlePositionArray[dimensionToUpdateItem] = populationArray[selectedFoodSourceID, dimensionToUpdateItem]
                                                                                + phiValue * distanceBetweenFoodSources;
                        }


                        //check if newly food sources within the serach space boundary
                        if (currentParticlePositionArray[dimensionToUpdateItem] > benchmarkFunction.SearchSpaceMaxValue[dimensionToUpdateItem])
                        {
                            currentParticlePositionArray[dimensionToUpdateItem] = benchmarkFunction.SearchSpaceMaxValue[dimensionToUpdateItem];
                        }
                        if (currentParticlePositionArray[dimensionToUpdateItem] < benchmarkFunction.SearchSpaceMinValue[dimensionToUpdateItem])
                        {
                            currentParticlePositionArray[dimensionToUpdateItem] = benchmarkFunction.SearchSpaceMinValue[dimensionToUpdateItem];
                        }
                    }

                    //Computing the cost of the current particle
                    double currentParticleValueObjectiveFunction = benchmarkFunction.ComputeValue(currentParticlePositionArray, ref currentNumberofunctionEvaluation, ShiftObjectiveFunctionOptimumValueToZero);



                    //Checking if the FunctionValueSigmaTolerance has been reached
                    if (FunctionValueSigmaTolerance > Math.Abs(currentParticleValueObjectiveFunction))
                    {
                        currentParticleValueObjectiveFunction = 0;
                        //if (currentObjectiveFunctionOptimum == 0) { optimumReached = true; }
                    }

                    //Computing the fitness
                    double currentParticleFitness = currentParticleValueObjectiveFunction.ComputeFitness(optimizationType);

                    TotalMutationCount++;


                    //Apply the greedy selection on newly generated food source
                    if (currentParticleFitness > fitnessArray[selectedFoodSourceID])
                    {
                        //Newly generated food source is better
                        for (int arrayColumn = 0; arrayColumn < ProblemDimension; arrayColumn++)
                        {
                            populationArray[selectedFoodSourceID, arrayColumn] = currentParticlePositionArray[arrayColumn];
                        }

                        valueObjectiveFunctionArray[selectedFoodSourceID] = currentParticleValueObjectiveFunction;
                        fitnessArray[selectedFoodSourceID] = currentParticleFitness;

                        limitValueForFoodSourcesArray[selectedFoodSourceID] = 0;
                        SuccessfullMutationCount++;

                        //Updating the GlobalBest Position
                        if (globalBestFitness < currentParticleFitness)
                        {
                            for (int arrayColumn = 0; arrayColumn < ProblemDimension; arrayColumn++)
                            {
                                globalBestPosition[arrayColumn] = populationArray[selectedFoodSourceID, arrayColumn];
                            }
                            globalBestFitness = currentParticleFitness;
                            globalBestValueObjectiveFunction = currentParticleValueObjectiveFunction;



                            //Check whether the optimization process should be
                            //stopped if the optimal value has been reached
                            if (StopOptimizationWhenOptimumIsReached == true && (Math.Abs(currentObjectiveFunctionOptimum - globalBestValueObjectiveFunction) < FunctionValueSigmaTolerance || Math.Abs(currentObjectiveFunctionOptimum - globalBestValueObjectiveFunction) < double.MinValue))
                            {
                                TotalMutationCountList.Add(TotalMutationCount);
                                SuccessfullMutationCountList.Add(SuccessfullMutationCount);

                                //collect current best function eval list for post data analysis 
                                bestObjectiveFunctionEvaluationData.Add(globalBestValueObjectiveFunction);

                                PrepareResultData(resultsData, MHOptimizationResult.OptimalFunctionValue, globalBestValueObjectiveFunction);
                                PrepareResultData(resultsData, MHOptimizationResult.NumberOfFunctionEvaluation, currentNumberofunctionEvaluation);
                                PrepareResultData(resultsData, MHOptimizationResult.NumberOfTotalIteration, iterationNumber);
                                PrepareResultData(resultsData, MHOptimizationResult.OptimalPoint, globalBestPosition);
                                PrepareResultData(resultsData, MHOptimizationResult.ExecutionTime, watch.ElapsedMilliseconds);
                                PrepareResultData(resultsData, MHOptimizationResult.OptimumFound, true);
                                PrepareResultData(resultsData, MHOptimizationResult.TotalMutationCountData, new List<int>());
                                PrepareResultData(resultsData, MHOptimizationResult.TotalSuccessfullMutationCountData, new List<int>());
                                PrepareResultData(resultsData, MHOptimizationResult.ObjectiveFunctionEvaluationData, bestObjectiveFunctionEvaluationData);

                                return;
                            }
                        }
                    }
                    else
                    {
                        //newly generated food source is not better
                        //increase the limit value counter for current food source
                        limitValueForFoodSourcesArray[selectedFoodSourceID]++;
                    }

                    //Check to see whether we have depleted the number of allowed function evaluation
                    if (currentNumberofunctionEvaluation > maxFunctionEvaluationNumber)
                    {
                        TotalMutationCountList.Add(TotalMutationCount);
                        SuccessfullMutationCountList.Add(SuccessfullMutationCount);

                        //collect current best function eval list for post data analysis 
                        if (currentNumberofunctionEvaluation % 250000 != 0) bestObjectiveFunctionEvaluationData.Add(globalBestValueObjectiveFunction);

                        PrepareResultData(resultsData, MHOptimizationResult.OptimalFunctionValue, globalBestValueObjectiveFunction);
                        PrepareResultData(resultsData, MHOptimizationResult.NumberOfFunctionEvaluation, currentNumberofunctionEvaluation);
                        PrepareResultData(resultsData, MHOptimizationResult.NumberOfTotalIteration, iterationNumber);
                        PrepareResultData(resultsData, MHOptimizationResult.OptimalPoint, globalBestPosition);
                        PrepareResultData(resultsData, MHOptimizationResult.ExecutionTime, watch.ElapsedMilliseconds);
                        PrepareResultData(resultsData, MHOptimizationResult.OptimumFound, false);
                        PrepareResultData(resultsData, MHOptimizationResult.TotalMutationCountData, new List<int>());
                        PrepareResultData(resultsData, MHOptimizationResult.TotalSuccessfullMutationCountData, new List<int>());
                        PrepareResultData(resultsData, MHOptimizationResult.ObjectiveFunctionEvaluationData, bestObjectiveFunctionEvaluationData);

                        return;
                    }
                }
                #endregion



                #region Scout bee phase
                //Locating the food source with maximum limit value
                int maxLimitValueForFoodSources = 0;
                int maxLimitValueForFoodSourcesID = 0;

                for (int foodSourceID = 0; foodSourceID < nbrEmployed; foodSourceID++)
                {
                    if (limitValueForFoodSourcesArray[foodSourceID] > maxLimitValueForFoodSources)
                    {
                        maxLimitValueForFoodSources = limitValueForFoodSourcesArray[foodSourceID];
                        maxLimitValueForFoodSourcesID = foodSourceID;
                    }
                }

                //Check to see if the 'maxLimitValueForFoodSources' has exceeded the 'limit' parameter
                if (maxLimitValueForFoodSources > limitValue)
                {
                    // Replace the 'maxLimitValueForFoodSourcesID' food source with a ramdomly generated one
                    switch (scoutGenerationScheme)
                    {
                        case ScoutGenerationType.Random:
                            for (int arrayColumn = 0; arrayColumn < ProblemDimension; arrayColumn++)
                            {
                                populationArray[maxLimitValueForFoodSourcesID, arrayColumn] = searchSpaceMinValue[arrayColumn] + ((double)rand.NextDouble() * searchSpaceRangeValue[arrayColumn]);
                                limitValueForFoodSourcesArray[maxLimitValueForFoodSourcesID] = 0;
                            }
                            break;

                        //The new source (scout) will be genrated by computing the mean of all other vurrent solutions
                        case ScoutGenerationType.MeanExistingSolution:
                            for (int arrayColumn = 0; arrayColumn < ProblemDimension; arrayColumn++)
                            {
                                populationArray[maxLimitValueForFoodSourcesID, arrayColumn] = 0;

                                for (int j_SolutionItem = 0; j_SolutionItem < nbrEmployed; j_SolutionItem++)
                                {
                                    if (maxLimitValueForFoodSourcesID != j_SolutionItem)
                                    {
                                        populationArray[maxLimitValueForFoodSourcesID, arrayColumn] += populationArray[j_SolutionItem, arrayColumn];
                                    }
                                }

                                populationArray[maxLimitValueForFoodSourcesID, arrayColumn] = populationArray[maxLimitValueForFoodSourcesID, arrayColumn] / ((double)nbrEmployed - 1.0);
                            }
                            limitValueForFoodSourcesArray[maxLimitValueForFoodSourcesID] = 0;

                            break;

                        default:
                            for (int arrayColumn = 0; arrayColumn < ProblemDimension; arrayColumn++)
                            {
                                populationArray[maxLimitValueForFoodSourcesID, arrayColumn] = searchSpaceMinValue[arrayColumn] + ((double)rand.NextDouble() * searchSpaceRangeValue[arrayColumn]);
                                limitValueForFoodSourcesArray[maxLimitValueForFoodSourcesID] = 0;
                            }

                            break;
                    }


                    //Resetting exploitation count data for replaced food source
                    foodSourceExploitationCountArray[maxLimitValueForFoodSourcesID] = 0;


                    //Computing the cost of the newly generated  food sources
                    double[] currentParticlePositionArray = new double[ProblemDimension];

                    //Obtaining the position of current particle
                    for (int arrayColumn = 0; arrayColumn < ProblemDimension; arrayColumn++)
                    {
                        currentParticlePositionArray[arrayColumn] = populationArray[maxLimitValueForFoodSourcesID, arrayColumn];
                    }

                    //Computing the cost of the current particle
                    valueObjectiveFunctionArray[maxLimitValueForFoodSourcesID] = benchmarkFunction.ComputeValue(currentParticlePositionArray, ref currentNumberofunctionEvaluation, ShiftObjectiveFunctionOptimumValueToZero);



                    //Checking if the FunctionValueSigmaTolerance has been reached
                    if (FunctionValueSigmaTolerance > Math.Abs(valueObjectiveFunctionArray[maxLimitValueForFoodSourcesID]))
                    {
                        valueObjectiveFunctionArray[maxLimitValueForFoodSourcesID] = 0;
                    }


                    //Computing the fitness
                    fitnessArray[maxLimitValueForFoodSourcesID] = valueObjectiveFunctionArray[maxLimitValueForFoodSourcesID].ComputeFitness(optimizationType);

                    //Updating the GlobalBest Position
                    if (globalBestFitness < fitnessArray[maxLimitValueForFoodSourcesID])
                    {
                        for (int arrayColumn = 0; arrayColumn < ProblemDimension; arrayColumn++)
                        {
                            globalBestPosition[arrayColumn] = populationArray[maxLimitValueForFoodSourcesID, arrayColumn];
                        }
                        globalBestValueObjectiveFunction = valueObjectiveFunctionArray[maxLimitValueForFoodSourcesID];
                        globalBestFitness = fitnessArray[maxLimitValueForFoodSourcesID];


                        //Check whether the optimization process should be
                        //stopped if the optimal value has been reached
                        if (StopOptimizationWhenOptimumIsReached == true && (Math.Abs(currentObjectiveFunctionOptimum - globalBestValueObjectiveFunction) < FunctionValueSigmaTolerance || Math.Abs(currentObjectiveFunctionOptimum - globalBestValueObjectiveFunction) < double.MinValue))
                        {
                            TotalMutationCountList.Add(TotalMutationCount);
                            SuccessfullMutationCountList.Add(SuccessfullMutationCount);

                            //collect current best function eval list for post data analysis 
                            bestObjectiveFunctionEvaluationData.Add(globalBestValueObjectiveFunction);

                            PrepareResultData(resultsData, MHOptimizationResult.OptimalFunctionValue, globalBestValueObjectiveFunction);
                            PrepareResultData(resultsData, MHOptimizationResult.NumberOfFunctionEvaluation, currentNumberofunctionEvaluation);
                            PrepareResultData(resultsData, MHOptimizationResult.NumberOfTotalIteration, iterationNumber);
                            PrepareResultData(resultsData, MHOptimizationResult.OptimalPoint, globalBestPosition);
                            PrepareResultData(resultsData, MHOptimizationResult.ExecutionTime, watch.ElapsedMilliseconds);
                            PrepareResultData(resultsData, MHOptimizationResult.OptimumFound, true);
                            PrepareResultData(resultsData, MHOptimizationResult.TotalMutationCountData, new List<int>());
                            PrepareResultData(resultsData, MHOptimizationResult.TotalSuccessfullMutationCountData, new List<int>());
                            PrepareResultData(resultsData, MHOptimizationResult.ObjectiveFunctionEvaluationData, bestObjectiveFunctionEvaluationData);

                            return;
                        }
                    }

                    //collect current best function eval list for post data analysis 
                    if (currentNumberofunctionEvaluation % 250000 == 0) bestObjectiveFunctionEvaluationData.Add(globalBestValueObjectiveFunction);

                    //Check to see whether we have depleted the number of allowed function evaluation
                    if (currentNumberofunctionEvaluation > maxFunctionEvaluationNumber)
                    {
                        TotalMutationCountList.Add(TotalMutationCount);
                        SuccessfullMutationCountList.Add(SuccessfullMutationCount);

                        //collect current best function eval list for post data analysis 
                        if (currentNumberofunctionEvaluation % 250000 != 0) bestObjectiveFunctionEvaluationData.Add(globalBestValueObjectiveFunction);

                        PrepareResultData(resultsData, MHOptimizationResult.OptimalFunctionValue, globalBestValueObjectiveFunction);
                        PrepareResultData(resultsData, MHOptimizationResult.NumberOfFunctionEvaluation, currentNumberofunctionEvaluation);
                        PrepareResultData(resultsData, MHOptimizationResult.NumberOfTotalIteration, iterationNumber);
                        PrepareResultData(resultsData, MHOptimizationResult.OptimalPoint, globalBestPosition);
                        PrepareResultData(resultsData, MHOptimizationResult.ExecutionTime, watch.ElapsedMilliseconds);
                        PrepareResultData(resultsData, MHOptimizationResult.OptimumFound, false);
                        PrepareResultData(resultsData, MHOptimizationResult.TotalMutationCountData, new List<int>());
                        PrepareResultData(resultsData, MHOptimizationResult.TotalSuccessfullMutationCountData, new List<int>());
                        PrepareResultData(resultsData, MHOptimizationResult.ObjectiveFunctionEvaluationData, bestObjectiveFunctionEvaluationData);

                        return;
                    }
                }

                #endregion



                TotalMutationCountList.Add(TotalMutationCount);
                SuccessfullMutationCountList.Add(SuccessfullMutationCount);






                #region Parameters Tuning Process
                //Check to see if the tuning process should be executed
                if ((iterationNumber + 1) % AEEABC_NumberOfIterationsToTuneParameters == 0 && numberOfAllTunedParameter != 0)
                {
                    nbreDimByIteratList.Add(AEEABC_NumberDimensionToUpdate);
                    nbreGBestDimByIteratList.Add(AEEABC_NumberOfDimensionUsingGBest);

                    //Getting the mutation successfull rate for this tuning cycle
                    int totalMutationTuning = 0;
                    int totalSuccessfullMutationTuning = 0;
                    IndexAllParametersToTuneCurrentIterationList = new List<int>();


                    if (iterationNumber < AEEABC_NumberOfIterationsToTuneParameters)
                    {
                        totalMutationTuning = TotalMutationCountList[iterationNumber];
                        totalSuccessfullMutationTuning = SuccessfullMutationCountList[iterationNumber];
                    }
                    else
                    {
                        totalMutationTuning = TotalMutationCountList[iterationNumber] - TotalMutationCountList[iterationNumber - AEEABC_NumberOfIterationsToTuneParameters];
                        totalSuccessfullMutationTuning = SuccessfullMutationCountList[iterationNumber] - SuccessfullMutationCountList[iterationNumber - AEEABC_NumberOfIterationsToTuneParameters];
                    }

                    successfullMutationsRates.Add((double)totalSuccessfullMutationTuning / (double)totalMutationTuning);

                    //Getting the mutation rate data for this cycle and the previous
                    currentCycleSuccessfullMutationRate = successfullMutationsRates.Last();

                    if (successfullMutationsRates.Count == 1)
                    {
                        previousCycleSuccessfullMutationRate = 0;
                    }
                    else
                    {
                        previousCycleSuccessfullMutationRate = successfullMutationsRates[successfullMutationsRates.Count - 2];
                    }


                    if (currentCycleSuccessfullMutationRate < previousCycleSuccessfullMutationRate && changeindexCurrentParameterToTune == false)
                    {//current successfull mutatio rate is worst than the previous value
                     //Inverse the direction
                        currentSuccessMutaRateBetter = false;

                        if (tunedParameterDirectionList[indexCurrentParameterToTune] == OrderingType.Ascending)
                        {
                            tunedParameterDirectionList[indexCurrentParameterToTune] = OrderingType.Descending;
                        }
                        else
                        {
                            tunedParameterDirectionList[indexCurrentParameterToTune] = OrderingType.Ascending;
                        }

                        //Current parameter need to be tuned to restore original value
                        IndexAllParametersToTuneCurrentIterationList.Add(indexCurrentParameterToTune);


                        // prepare the next parameter to be tned in the next uning cycle
                        if (indexCurrentParameterToTune + 1 >= numberOfAllTunedParameter)
                        { //We have arrived at the last element in the list. Add the first
                            indexCurrentParameterToTune = 0;
                            if (IndexAllParametersToTuneCurrentIterationList.Contains(indexCurrentParameterToTune) == false) IndexAllParametersToTuneCurrentIterationList.Add(indexCurrentParameterToTune);
                        }
                        else
                        {//Add next element in the list
                            indexCurrentParameterToTune++;
                            IndexAllParametersToTuneCurrentIterationList.Add(indexCurrentParameterToTune);
                        }

                        changeindexCurrentParameterToTune = true;
                    }
                    else
                    {//Current successfull mutatio rate is better than the previous value
                     //Change next parameter
                        currentSuccessMutaRateBetter = true;

                        // prepare the next parameter to be tned in the next uning cycle
                        if (indexCurrentParameterToTune + 1 >= numberOfAllTunedParameter)
                        { //We have arrived at the last element in the list. Add the first
                            indexCurrentParameterToTune = 0;
                            if (IndexAllParametersToTuneCurrentIterationList.Contains(indexCurrentParameterToTune) == false) IndexAllParametersToTuneCurrentIterationList.Add(indexCurrentParameterToTune);
                        }
                        else
                        {//Add next element in the list
                            indexCurrentParameterToTune++;
                            IndexAllParametersToTuneCurrentIterationList.Add(indexCurrentParameterToTune);
                        }
                        changeindexCurrentParameterToTune = false;
                    }



                    //Browse the 'IndexAllParametersToTuneCurrentIterationList' to tune all required parameters
                    foreach (var indexParameterToTuneCurrentIterationItem in IndexAllParametersToTuneCurrentIterationList)
                    {
                        switch (NameAvailableParametersToTuneList[indexParameterToTuneCurrentIterationItem])
                        {
                            case AEEABCtunningParametersNames.NumberOfDimensionUsingGBest:
                                if (changeindexCurrentParameterToTune == true)
                                {//Restore previos value
                                    changeindexCurrentParameterToTune = false;
                                    AEEABC_NumberOfDimensionUsingGBest = (int)originalParameterValueBeforeUpdate;
                                }
                                else
                                {//Update current value based on the change direction
                                    originalParameterValueBeforeUpdate = AEEABC_NumberOfDimensionUsingGBest;

                                    if (tunedParameterDirectionList[indexParameterToTuneCurrentIterationItem] == OrderingType.Ascending) AEEABC_NumberOfDimensionUsingGBest++;
                                    if (tunedParameterDirectionList[indexParameterToTuneCurrentIterationItem] == OrderingType.Descending) AEEABC_NumberOfDimensionUsingGBest--;
                                }

                                if (AEEABC_NumberOfDimensionUsingGBest < 1) AEEABC_NumberOfDimensionUsingGBest = 1;
                                if (AEEABC_NumberOfDimensionUsingGBest > ProblemDimension) AEEABC_NumberOfDimensionUsingGBest = ProblemDimension;

                                break;
                            case AEEABCtunningParametersNames.Coeff3Value:
                                if (changeindexCurrentParameterToTune == true)
                                {//Restore previos value
                                    changeindexCurrentParameterToTune = false;
                                    AEEABC_Coeff3Value = originalParameterValueBeforeUpdate;
                                }
                                else
                                {//Update current value based on the change direction
                                    originalParameterValueBeforeUpdate = AEEABC_Coeff3Value;

                                    if (tunedParameterDirectionList[indexParameterToTuneCurrentIterationItem] == OrderingType.Ascending) AEEABC_Coeff3Value = AEEABC_Coeff3Value + AEEABC_Coeff3ValueChange;
                                    if (tunedParameterDirectionList[indexParameterToTuneCurrentIterationItem] == OrderingType.Descending) AEEABC_Coeff3Value = AEEABC_Coeff3Value - AEEABC_Coeff3ValueChange;

                                    if (AEEABC_Coeff3Value < 0.0d) AEEABC_Coeff3Value = 0d;
                                    if (AEEABC_Coeff3Value > 2.0d)
                                        AEEABC_Coeff3Value = 2.0d;

                                }
                                break;

                            case AEEABCtunningParametersNames.ScoutGenerationType:
                                if (currentSuccessMutaRateBetter == false)
                                {
                                    if (scoutGenerationScheme == ScoutGenerationType.Random)
                                    {
                                        scoutGenerationScheme = ScoutGenerationType.MeanExistingSolution;
                                    }
                                    else
                                    {
                                        scoutGenerationScheme = ScoutGenerationType.Random;
                                    }
                                }

                                break;



                            case AEEABCtunningParametersNames.ProbabilityEquationType:
                                if (currentSuccessMutaRateBetter == false)
                                {
                                    if (ABC_ProbabilityEquationType == ABC_ProbabilityEquationType.ComplementOriginal)
                                    {
                                        ABC_ProbabilityEquationType = ABC_ProbabilityEquationType.Original;
                                    }
                                    else
                                    {
                                        ABC_ProbabilityEquationType = ABC_ProbabilityEquationType.ComplementOriginal;
                                    }
                                }

                                break;



                            default:
                                break;
                        }
                    }

                   

                }
                #endregion


            }
            #endregion


            TotalMutationCountList.Add(TotalMutationCount);
            SuccessfullMutationCountList.Add(SuccessfullMutationCount);

            PrepareResultData(resultsData, MHOptimizationResult.OptimalFunctionValue, globalBestValueObjectiveFunction);
            PrepareResultData(resultsData, MHOptimizationResult.NumberOfFunctionEvaluation, currentNumberofunctionEvaluation);
            PrepareResultData(resultsData, MHOptimizationResult.NumberOfTotalIteration, maxItertaionNumber - 1);
            PrepareResultData(resultsData, MHOptimizationResult.OptimalPoint, globalBestPosition);
            PrepareResultData(resultsData, MHOptimizationResult.ExecutionTime, watch.ElapsedMilliseconds);
            PrepareResultData(resultsData, MHOptimizationResult.OptimumFound, false);
            PrepareResultData(resultsData, MHOptimizationResult.TotalMutationCountData, new List<int>());
            PrepareResultData(resultsData, MHOptimizationResult.TotalSuccessfullMutationCountData, new List<int>());
            PrepareResultData(resultsData, MHOptimizationResult.ObjectiveFunctionEvaluationData, bestObjectiveFunctionEvaluationData);

            return;
        }




        /// <summary>
        /// Call this method to prepare the data at the end of an optimization process
        /// </summary>
        public void PrepareResultData(List<OptimizationResultModel> data, MHOptimizationResult dataName, object dataValue)
        {
            if (data.Exists(x => x.Name == dataName) == true)
            {
                data.First(x => x.Name == dataName).Value = dataValue;
            }
            else
            {
                data.Add(new OptimizationResultModel(dataName, dataValue));
            }
        }



        /// <summary>
        /// Call this method to make hard personal copy of reference type 'optimizationConfiguration'
        /// </summary>
        /// <param name="optimizationConfiguration">the list of optimization parameters to apply to the current instance of metaheuristic optimization algorithm</param>
        /// <param name="description">the description of the current instance of metaheuristic optimization algorithm</param>
        /// <param name="randomIntValue"></param>
        public void MakePersonalOptimizationConfigurationListCopy(List<OptimizationParameter> optimizationConfiguration, string description, int randomIntValue)
        {
            //Making a distinct personal copy of the parameter list
            OptimizationConfiguration = new();
            OptimizationParameter parameter;


            foreach (var ParameterItem in optimizationConfiguration)
            {
                parameter = new OptimizationParameter(ParameterItem.Name, ParameterItem.Value, ParameterItem.IsEssentialInfo);
                OptimizationConfiguration.Add(parameter);
            }


            InstanceID = randomIntValue;
            Description = description;
        }





        /// <summary>
        /// Call this method to generate a random index from '0' to 'maxValue' while ignoring the index selected in 'indexToIgnore'
        /// </summary>
        /// <param name="maxValue">the maximal value the index could take</param>
        /// <param name="indexToIgnore">the list of indexes to ignore while computing the random one. the List could have a null value </param>
        /// <param name="numberofValuesToGenerate">The number of generated values to be returned as array of ints </param>
        /// <returns>a random zero based list of indexes</returns>
        private List<int> FindZeroBasedRandomIndex(int maxValue, List<int> indexesToIgnoreList, Random rand, int numberofValuesToGenerate)
        {
            //List<int> indexList = new();
            List<int> indexList = new();
            List<int> resultList = new();

            for (int i = 0; i <= maxValue; i++)
            {
                if (indexesToIgnoreList != null && indexesToIgnoreList.Contains(i) == false)
                {
                    indexList.Add(i);
                }
            }

            int tempVal;

            for (int i = 0; i < numberofValuesToGenerate; i++)
            {
                tempVal = (int)Math.Floor(rand.NextDouble() * indexList.Count);
                resultList.Add(indexList[tempVal]);

                indexList.RemoveAt(tempVal);
            }

            return resultList;
        }

        private void GenerateListForStandardDimandGbestDim(int maxValue, Random rand, int numberStandardDim, List<int> listStDim, int numberGBestDim, List<int> listGBDim)
        {
            //List<int> indexList = new();
            List<int> indexList = new();

            for (int i = 0; i <= maxValue; i++)
            {
                indexList.Add(i);
            }

            int tempVal;


            for (int i = 0; i < numberStandardDim; i++)
            {
                tempVal = (int)Math.Floor(rand.NextDouble() * indexList.Count);
                listStDim.Add(indexList[tempVal]);

                indexList.RemoveAt(tempVal);
            }

            for (int i = 0; i < numberGBestDim; i++)
            {
                tempVal = (int)Math.Floor(rand.NextDouble() * indexList.Count);
                listGBDim.Add(indexList[tempVal]);
            }


        }



        /// <summary>
        /// Call this method to generate a random value from a list of values while ignoring the index selected in 'indexToIgnore'
        /// </summary>
        /// <param name="valuesToChooseFrom">The list to choose a random value from</param>
        /// <param name="indexToIgnore">the list of indexes to ignore while computing the random one. the List could have a null value </param>
        /// <returns>a random zero based index</returns>
        private int FindZeroBasedRandomIndex(List<int> valuesToChooseFrom, List<int> indexesToIgnoreList, int randomIntValue)
        {
            List<int> tempvaluesToChooseFrom = new();

            foreach (var valueItem in valuesToChooseFrom)
            {
                if (indexesToIgnoreList.Contains(valueItem) == false)
                {
                    tempvaluesToChooseFrom.Add(valueItem);
                }
            }

            Random rand = new Random(randomIntValue);
            int randomIndex = (int)Math.Floor(rand.NextDouble() * tempvaluesToChooseFrom.Count);

            return tempvaluesToChooseFrom[randomIndex];
        }


    }
}
