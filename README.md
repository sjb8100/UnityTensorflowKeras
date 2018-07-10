# UnityTensorflowKeras
Still in development. Don't use this unless you know what you have time to check the sourcecodes!

- This repo is made for Aalto University's Computational Intelligence in Games course. [The original materials](https://github.com/tcmxx/CNTKUnityTools) are made with [CNTK](https://github.com/Microsoft/CNTK). Now it is remade with [Tensorflow](https://github.com/tensorflow/tensorflow).

- It is an extension of [Unity ML agent](https://github.com/Unity-Technologies/ml-agents) for deep learning, primarily reinforcement learning, with in-editor/in-game training support. It also provides interface for another optimization algorithm called MAES.

- It uses a modified version of [KerasSharp](https://github.com/tcmxx/keras-sharp) and [TensorflowSharp](https://github.com/migueldeicaza/TensorFlowSharp) as backend. No python is needed for model building/evaluation/training. 

## Installation
1. Clone the Unity MLAgent repo: https://github.com/Unity-Technologies/ml-agents
2. Import the TenfowflowSharp plugin. One provided by Unity: https://github.com/Unity-Technologies/ml-agents/blob/master/docs/Using-TensorFlow-Sharp-in-Unity.md. This is not fully updated. A new version will be provided soon.
3. Clone this repo with submodules, for example you can use: 
 ```
 git clone --recursive https://github.com/tcmxx/UnityTensorflowKeras.git
 ```
4. Copy everything in UnityTensorflowKeras's Assets folder and put it into Unity MLAgent's Assets folder. Replace the files that already exist.
5. Done!

## Platforms:
Mac and Windows are fully supported. Android does not support training. IOS is not tested.
Nothing is fully tested yet.