﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Linq;
using MLAgents;

/// CoreBrain which decides actions using internally embedded TensorFlow model.
public class CoreBrainInternalTrainable : ScriptableObject, CoreBrain
{
    /// Reference to the brain that uses this CoreBrainInternal
    public Brain brain;
    public GameObject trainer;

    protected ITrainer trainerInterface;
    private Dictionary<Agent, AgentInfo> currentInfo;
    private Dictionary<Agent, TakeActionOutput> prevActionOutput;






    /// Create the reference to the brain
    public void SetBrain(Brain b)
    {
        brain = b;
        if (trainer)
        {
            trainerInterface = trainer.GetComponent<ITrainer>();
            Debug.Assert(trainerInterface != null, "Please make sure your trainer has a monobehaviour that implement ITrainer interface attached!");
            trainerInterface?.SetBrain(b);
        }
    }


    public void InitializeCoreBrain(MLAgents.Batcher brainBatcher)
    {
        Debug.Assert(trainer != null && trainerInterface != null, "Please specify a trainer in the Trainer field of your Brain!");
        trainerInterface.Initialize();
    }



    /// Uses the stored information to run the tensorflow graph and generate 
    /// the actions.
    public void DecideAction(Dictionary<Agent, AgentInfo> newAgentInfos)
    {
        int currentBatchSize = newAgentInfos.Count();
        List<Agent> newAgentList = newAgentInfos.Keys.ToList();
        List<Agent> recordableAgentList = newAgentList.Where((a) => currentInfo != null && currentInfo.ContainsKey(a) && prevActionOutput.ContainsKey(a)).ToList();

        if (currentBatchSize == 0)
        {
            return;
        }


        //get the datas only for the agents in the agentInfo input
        var prevInfo = GetValueForAgents(currentInfo, recordableAgentList);    
        var prevActionActions = GetValueForAgents(prevActionOutput, recordableAgentList);
        var newInfo = GetValueForAgents(newAgentInfos, recordableAgentList);

        if (recordableAgentList.Count > 0 && trainerInterface.IsTraining() && trainerInterface.GetStep() <= trainerInterface.GetMaxStep())
        {
            trainerInterface.AddExperience(prevInfo, newInfo, prevActionActions);
            trainerInterface.ProcessExperience(prevInfo, newInfo);
        }



        if (trainerInterface.IsTraining() && trainerInterface.GetStep() <= trainerInterface.GetMaxStep())
        {
            trainerInterface.IncrementStep();
        }

        //update the info
        UpdateInfos(ref currentInfo, newAgentInfos);

        var actionOutputs = trainerInterface.TakeAction(GetValueForAgents(currentInfo, newAgentList));
        UpdateActionOutputs(ref prevActionOutput, actionOutputs);

        //TODO Update the agent's other info if there is
        foreach (Agent agent in newAgentList)
        {
            if (actionOutputs.ContainsKey(agent) && actionOutputs[agent].outputAction != null)
                agent.UpdateVectorAction(trainerInterface.PostprocessingAction(actionOutputs[agent].outputAction));
        }



        if (trainerInterface.IsReadyUpdate() && trainerInterface.IsTraining() && trainerInterface.GetStep() <= trainerInterface.GetMaxStep())
        {
            trainerInterface.UpdateModel();
        }

        //clear the prev record if the agent is done
        foreach (Agent agent in newAgentList)
        {
            if(newAgentInfos[agent].done || newAgentInfos[agent].maxStepReached)
            {
                currentInfo.Remove(agent);
            }
        }

    }

    /// Displays the parameters of the CoreBrainInternal in the Inspector 
    public void OnInspector()
    {
#if UNITY_EDITOR
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        var serializedBrain = new SerializedObject(this);



        var trainerProperty = serializedBrain.FindProperty("trainer");
        serializedBrain.Update();
        EditorGUILayout.PropertyField(trainerProperty, true);
        serializedBrain.ApplyModifiedProperties();
#endif
    }



    protected static Dictionary<Agent, T> GetValueForAgents<T>(Dictionary<Agent, T> allInfos, List<Agent> agents)
    {


        Dictionary<Agent, T> result = new Dictionary<Agent, T>();
        foreach (var agent in agents)
        {
            result[agent] = allInfos[agent];
        }
        return result;
    }

    protected static void UpdateInfos(ref Dictionary<Agent, AgentInfo> allInfos, Dictionary<Agent, AgentInfo> newInfos)
    {
        if (allInfos == null)
            allInfos = new Dictionary<Agent, AgentInfo>();

        foreach (var agent in newInfos.Keys)
        {

            //TODO remove this once Unity fixed their texture not released bug
            if (allInfos.ContainsKey(agent))
            {
                foreach (var v in allInfos[agent].visualObservations)
                {
                    Destroy(v);
                }
            }

            allInfos[agent] = CopyAgentInfo(newInfos[agent]);
        }
    }

    protected static void UpdateActionOutputs(ref Dictionary<Agent, TakeActionOutput> actionOutputs, Dictionary<Agent, TakeActionOutput> newActionOutputs)
    {
        if (actionOutputs == null)
            actionOutputs = new Dictionary<Agent, TakeActionOutput>();

        foreach (var agent in newActionOutputs.Keys)
        {
            actionOutputs[agent] = newActionOutputs[agent];
        }
    }

    public static AgentInfo CopyAgentInfo(AgentInfo agentInfo)
    {
        var result = new AgentInfo()
        {
            vectorObservation = new List<float>(agentInfo.vectorObservation),
            stackedVectorObservation = new List<float>(agentInfo.stackedVectorObservation),
            visualObservations = new List<Texture2D>(agentInfo.visualObservations),
            textObservation = (string)agentInfo.textObservation?.Clone(),
            storedVectorActions = (float[])agentInfo.storedVectorActions.Clone(),
            storedTextActions = (string)agentInfo.storedTextActions?.Clone(),
            memories = new List<float>(agentInfo.memories),
            reward = agentInfo.reward,
            done = agentInfo.done,
            maxStepReached = agentInfo.maxStepReached,
            id = agentInfo.id
        };

        return result;
    }
}
