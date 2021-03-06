﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Accord.Math;
using MLAgents;
using System.IO;
using KerasSharp.Backends;
using System.Runtime.Serialization.Formatters.Binary;

public struct TakeActionOutput
{
    public float[] outputAction;
    public float[] allProbabilities; //used for RL
    public float value;//use for RL
    //public Dictionary<Agent, float[]> memory;
    //public Dictionary<Agent, string> textAction;
}

/// <summary>
/// Inplement this interface on any MonoBehaviour for your own trainer that can be used on CoreBrainInteralTrainable as a Trainer.
/// </summary>
public interface ITrainer
{
    /// <summary>
    /// THis will be called to give you the reference to the Brain.
    /// </summary>
    /// <param name="brain"></param>
    void SetBrain(Brain brain);

    /// <summary>
    /// impelment all of your initialization here
    /// </summary>
    void Initialize();

    /// <summary>
    /// Return the max steps of the training.
    /// </summary>
    /// <returns>max steps</returns>
    int GetMaxStep();

    /// <summary>
    /// return current steps.
    /// </summary>
    /// <returns>curren steps</returns>
    int GetStep();

    /// <summary>
    /// This will be called every fixed update when training is enabled.
    /// </summary>
    void IncrementStep();

    /// <summary>
    /// Reset your trainer
    /// </summary>
    void ResetTrainer();

    /// <summary>
    /// This will be called when an action on a agent is requested. Implement your logic to return the actions to take based on agent's current states.
    /// </summary>
    /// <param name="agentInfos">the information of agents that need actions.</param>
    /// <returns>a disionary of agent and its action to take</returns>
    Dictionary<Agent, TakeActionOutput> TakeAction(Dictionary<Agent, AgentInfo> agentInfos);

    /// <summary>
    /// This will be called every loop when when training is enabled. You should record the infos of the agents based on the need of your algorithm.
    /// </summary>
    /// <param name="currentInfo">infomation of the agents before the action taken.</param>
    /// <param name="newInfo">infomation of the agents after tha ction taken</param>
    /// <param name="actionOutput">the action taken</param>
    void AddExperience(Dictionary<Agent, AgentInfo> currentInfo, Dictionary<Agent, AgentInfo> newInfo, Dictionary<Agent, TakeActionOutput> actionOutput);

    /// <summary>
    /// Same as AddExperience(), called every loop when training. You are supposed to process the collected data for episodes or something. You can do it in AddExperience as well...This method is called right after AddExperience().
    /// </summary>
    /// <param name="currentInfo">infomation of the agents before the action taken.</param>
    /// <param name="newInfo">infomation of the agents after tha ction taken</param>
    void ProcessExperience(Dictionary<Agent, AgentInfo> currentInfo, Dictionary<Agent, AgentInfo> newInfo);

    /// <summary>
    /// When this returns true, UpdateModel() will be called();
    /// </summary>
    /// <returns>Whether it is ready to udpate the model.</returns>
    bool IsReadyUpdate();

    /// <summary>
    /// Put all of your logic for training the model. This is called when IsReadyUpdate()  returns true.
    /// </summary>
    void UpdateModel();

    /// <summary>
    /// Return whether training is enabled. AddExperience(), ProcessExperience() and UpdateModel() will not be called if it returns false.
    /// </summary>
    /// <returns></returns>
    bool IsTraining();


    /// <summary>
    /// The final action received by Agent will be post processed by this. The experience record will not be processed by this. Just return the input if you dont need any processing
    /// </summary>
    /// <returns>processed action</returns>
    float[] PostprocessingAction(float[] rawAction);
}


/// <summary>
/// A abstract class for trainer if you want to save some time impelmenting ITrainer...It provides some helper functions and stuff..., you can use this as based class instead of ITrainer.
/// </summary>
public abstract class Trainer : MonoBehaviour, ITrainer
{

    protected Academy academyRef;
    public LearningModelBase modelRef;
    public bool isTraining;
    protected bool prevIsTraining;

    public Action<bool> onIsTrainingChanged;

    [ShowAllPropertyAttr]
    public TrainerParams parameters;
    public bool continueFromCheckpoint = true;
    public string checkpointPath = @"Assets";
    public string checkpointFileName = @"testcheckpoint.bytes";
    private readonly string stepsSavingKey = "UnityTrainerSteps";

    [SerializeField]
    [Tooltip("Current steps of the trainer.")]
    protected int steps = 0;


    public Brain BrainToTrain { get; protected set; }

    private void Start()
    {
        academyRef = FindObjectOfType<Academy>();
        Debug.Assert(academyRef != null, "No Academy in this scene!");
        prevIsTraining = isTraining;
        academyRef.SetIsInference(!isTraining);
    }
    public virtual void Update()
    {
        if (prevIsTraining != isTraining)
        {
            onIsTrainingChanged?.Invoke(isTraining);
            prevIsTraining = isTraining;
            academyRef.SetIsInference(!isTraining);
        }
    }


    protected virtual void FixedUpdate()
    {
        if (BrainToTrain == null)
        {
            Debug.LogError("Please assign this trainer to a Brain with CoreBrainInternalTrainable!");
        }
        if (isTraining)
            modelRef.SetLearningRate(parameters.learningRate);

        /*if (IsReadyUpdate() && isTraining && GetStep() <= GetMaxStep())   //moved into CoreBrainInternalTrainable
        {
            UpdateModel();
        }*/
    }

    public virtual void SetBrain(Brain brain)
    {
        this.BrainToTrain = brain;
    }

    public abstract void Initialize();

    public virtual int GetMaxStep()
    {
        return parameters.maxTotalSteps;
    }

    public virtual int GetStep()
    {
        return steps;
    }
    public virtual void IncrementStep()
    {
        steps++;
        if (steps % parameters.saveModelInterval == 0)
        {
            SaveModel();
        }
    }

    public virtual void ResetTrainer()
    {
        steps = 0;
    }

    public virtual float[] PostprocessingAction(float[] rawAction)
    {
        return rawAction;
    }


    public abstract Dictionary<Agent, TakeActionOutput> TakeAction(Dictionary<Agent, AgentInfo> agentInfos);
    public abstract void AddExperience(Dictionary<Agent, AgentInfo> currentInfo, Dictionary<Agent, AgentInfo> newInfo, Dictionary<Agent, TakeActionOutput> actionOutput);
    public abstract void ProcessExperience(Dictionary<Agent, AgentInfo> currentInfo, Dictionary<Agent, AgentInfo> newInfo);
    public abstract bool IsReadyUpdate();
    public abstract void UpdateModel();


    /// <summary>
    /// save the model to the checkpoint path.
    /// </summary>
    public void SaveModel()
    {
        if (string.IsNullOrEmpty(checkpointFileName))
        {
            Debug.Log("checkpointFileName empty. model not saved.");
            return;
        }
        var dataDic = modelRef.SaveCheckpoint();
        dataDic[stepsSavingKey] = new int[] { steps };

        //serailzie the data and save it to path
        var binFormatter = new BinaryFormatter();
        var mStream = new MemoryStream();
        binFormatter.Serialize(mStream, dataDic);
        byte[] data = mStream.ToArray();
        var fullPath = Path.GetFullPath(Path.Combine(checkpointPath, checkpointFileName));
        fullPath = fullPath.Replace('/', Path.DirectorySeparatorChar);
        fullPath = fullPath.Replace('\\', Path.DirectorySeparatorChar);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        File.WriteAllBytes(fullPath, data);
        Debug.Log("Checkpoint saved to " + fullPath);
    }

    /// <summary>
    /// Load the model ffrom the checkpointpath
    /// </summary>
    public void LoadModel()
    {
        if (string.IsNullOrEmpty(checkpointFileName))
        {
            Debug.Log("checkpointFileName empty. model not loaded.");
            return;
        }
        string fullPath = Path.GetFullPath(Path.Combine(checkpointPath, checkpointFileName));
        fullPath = fullPath.Replace('/', Path.DirectorySeparatorChar);
        fullPath = fullPath.Replace('\\', Path.DirectorySeparatorChar);
        if (!File.Exists(fullPath))
        {
            Debug.Log("Model checkpoint not exist at: " + fullPath);
            return;
        }
        var bytes = File.ReadAllBytes(fullPath);
        var mStream = new MemoryStream(bytes);
        var binFormatter = new BinaryFormatter();
        var deserializedData = binFormatter.Deserialize(mStream);
        if (deserializedData is Dictionary<string, Array>)
        {
            var dataDic = deserializedData as Dictionary<string, Array>;
            modelRef.SetAllModelWeights(dataDic);
            modelRef.SetAllOptimizerWeights(dataDic);
            if (dataDic.ContainsKey(stepsSavingKey))
            {
                steps = (int)dataDic[stepsSavingKey].GetValue(0);
            }
            Debug.Log("Checkpoint loaded from " + fullPath);
        }
        else
        {
            Debug.LogError("Not recognized datatype to restoed from");
        }

        //load the datas for trainer

    }


    /// <summary>
    /// return the 3D float array of the texture image.
    /// </summary>
    /// <param name="tex">texture</param>
    /// <param name="blackAndWhite">whether return black and white</param>
    /// <returns>HWC array of the image</returns>
    public static float[,,] TextureToArray(Texture2D tex, bool blackAndWhite)
    {
        int width = tex.width;
        int height = tex.height;
        int pixels = 0;
        if (blackAndWhite)
            pixels = 1;
        else
            pixels = 3;
        float[,,] result = new float[height, width, pixels];
        float[] resultTemp = new float[height * width * pixels];
        int wp = width * pixels;

        Color32[] cc = tex.GetPixels32();
        for (int h = height - 1; h >= 0; h--)
        {
            for (int w = 0; w < width; w++)
            {
                Color32 currentPixel = cc[(height - h - 1) * width + w];
                if (!blackAndWhite)
                {
                    resultTemp[h * wp + w * pixels] = currentPixel.r / 255.0f;
                    resultTemp[h * wp + w * pixels + 1] = currentPixel.g / 255.0f;
                    resultTemp[h * wp + w * pixels + 2] = currentPixel.b / 255.0f;
                }
                else
                {
                    resultTemp[h * wp + w * pixels] =
                    (currentPixel.r + currentPixel.g + currentPixel.b)
                    / 3;
                }
            }
        }

        Buffer.BlockCopy(resultTemp, 0, result, 0, height * width * pixels * sizeof(float));
        return result;
    }

    /// <summary>
    /// Create the visual input batch that can be used directly to feed neural network for all agents's camera visual inputs.
    /// </summary>
    /// <param name="currentInfo">Agents and their infomation wiht visual texture data</param>
    /// <param name="agentList">List of agents that needs to be included in the output</param>
    /// <param name="cameraResolutions">camera resolution data. Should be obtain from the Brain.</param>
    /// <returns>List of visual input batch data. Each item in the list is for item in cameraResolution parameter</returns>
    public static List<float[,,,]> CreateVisualInputBatch(Dictionary<Agent, AgentInfo> currentInfo, List<Agent> agentList, resolution[] cameraResolutions)
    {
        if (cameraResolutions == null || cameraResolutions.Length <= 0)
            return null;
        if (currentInfo.Count <= 0 || agentList.Count <= 0)
            return null;
        var observationMatrixList = new List<float[,,,]>();
        var texturesHolder = new List<Texture2D>();

        for (int observationIndex = 0; observationIndex < cameraResolutions.Length; observationIndex++)
        {
            texturesHolder.Clear();
            foreach (Agent agent in agentList)
            {
                texturesHolder.Add(currentInfo[agent].visualObservations[observationIndex]);
            }
            observationMatrixList.Add(texturesHolder.BatchVisualObservations(cameraResolutions[observationIndex].blackAndWhite));
        }

        return observationMatrixList;
    }

    /// <summary>
    /// Create vector observation batch data  that can be used directly to feed neural network.
    /// </summary>
    /// <param name="currentInfo">Agents and their infomation with vector observation</param>
    /// <param name="agentList">List of agents that needs to be included in the output</param>
    /// <returns>bacth vector observation data.</returns>
    public static float[,] CreateVectorInputBatch(Dictionary<Agent, AgentInfo> currentInfo, List<Agent> agentList)
    {
        if (currentInfo.Count <= 0 || agentList.Count <= 0)
            return null;
        int obsSize = currentInfo[agentList[0]].stackedVectorObservation.Count;
        if (obsSize == 0)
            return null;
        var result = new float[agentList.Count, obsSize];

        int i = 0;
        foreach (Agent agent in agentList)
        {
            result.SetRow(i, currentInfo[agent].stackedVectorObservation.ToArray());
            i++;
        }

        return result;
    }

    public bool IsTraining()
    {
        return isTraining;
    }
}
