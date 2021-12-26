using System;
using System.Numerics;
using Newtonsoft.Json.Linq;

namespace MotionConverter
{
    public class PhysicsDataConverter
    {
        int phySettingCount;
        int outputCount;
        int inputCount;
        int vertexCount;

        JObject inputObject; // should be readonly
        public PhysicsDataConverter()
        {
            
        }

        public JObject Convert(JObject cubismPhysicsController)
        {
            // Initialize for each convert
            phySettingCount = 0;
            outputCount = 0;
            inputCount = 0;
            vertexCount = 0;
            JObject result = new JObject();
            inputObject = cubismPhysicsController.GetValue("0 MonoBehaviour Base") as JObject;
            inputObject = inputObject.GetValue("0 CubismPhysicsRig _rig") as JObject;
            
            // Process phy settings
            JArray subrigsArray = (inputObject.GetValue("0 CubismPhysicsSubRig SubRigs") as JObject).GetValue("0 Array Array") as JArray;
            JArray physicsSettings = GetSubRigs(subrigsArray);
            // Write
            WriteAll(result, GetMeta(), physicsSettings);
            return result;
        }

        void WriteAll(JObject data, JObject meta, JArray physicsSettings)
        {
            data.Add("Version", 3);
            data.Add("Meta", meta);
            data.Add("PhysicsSettings", physicsSettings);
        }
        JObject GetMeta()
        {
            JObject meta = new JObject();
            // statistics that we get during processing
            meta.Add("PhysicsSettingCount", phySettingCount);
            meta.Add("TotalInputCount", inputCount);
            meta.Add("TotalOutputCount", outputCount);
            meta.Add("VertexCount", vertexCount);
            // Effective Forces
            Vector2 gravity, wind;
            JObject gravityObj = inputObject.GetValue("0 Vector2f Gravity") as JObject;
            JObject windObj = inputObject.GetValue("0 Vector2f Wind") as JObject;
            gravity = new Vector2((float) gravityObj.GetValue("0 float x"), (float) gravityObj.GetValue("0 float y"));
            wind = new Vector2((float) windObj.GetValue("0 float x"), (float) windObj.GetValue("0 float y"));

            
            gravityObj = new JObject {{"X", gravity.X}, {"Y", gravity.Y}};
            windObj = new JObject {{"X", wind.X}, {"Y", wind.Y}};
            JObject effectiveForcesObj = new JObject{{"Gravity", gravityObj}, {"Wind", windObj}};
            meta.Add("EffectiveForces", effectiveForcesObj);
            // Physics Dictionary
            JArray physDictArray = new JArray();
            for (int i = 1; i < phySettingCount + 1; i++)
            {
                // Settings Count = Subrig Count. Index starts from 1
                // Name should not matter, I suppose.
                JObject dictEntry = new JObject {{"Id", $"PhysicsSetting{i}"}, {"Name", i==phySettingCount? $"{i}" : $"GeneratedByGFLL2DFadeMotionConverter{i}"}};
                physDictArray.Add(dictEntry);
            }
            meta.Add("PhysicsDictionary", physDictArray);
            return meta;
        }

        JArray GetSubRigs(JArray subrigs)
        {
            JArray physicsSettings = new JArray();
            foreach (var rigobj in subrigs)
            {
                phySettingCount++;
                JObject subrigData = (rigobj as JObject).GetValue("0 CubismPhysicsSubRig data") as JObject;
                JObject physicsSetting = new JObject();
                physicsSetting.Add("Id", $"PhysicsSetting{phySettingCount}");
                
                // Inputs
                JArray convertedInputs =
                    GetInputs(
                        (subrigData.GetValue("0 CubismPhysicsInput Input") as JObject).GetValue("0 Array Array") as
                        JArray);
                physicsSetting.Add("Input", convertedInputs);
                
                // Outputs
                JArray convertedOutputs = 
                    GetOutputs(
                        (subrigData.GetValue("0 CubismPhysicsOutput Output") as JObject).GetValue("0 Array Array") as
                        JArray);
                physicsSetting.Add("Output", convertedOutputs);
                
                // Vertices
                JArray convertedVertices = GetVertices(
                    (subrigData.GetValue("0 CubismPhysicsParticle Particles") as JObject).GetValue("0 Array Array") as
                    JArray);
                physicsSetting.Add("Vertices", convertedVertices);
                
                // Normalization
                JObject convertedNormalization = GetNormalization(subrigData.GetValue("0 CubismPhysicsNormalization Normalization") as JObject) as JObject;
                physicsSetting.Add("Normalization", convertedNormalization);
                physicsSettings.Add(physicsSetting);
            }

            return physicsSettings;
        }

        


        #region SubRig Part Conversion
        // Normalization Composes one JObject, while other three are all JArrays

        #region parsers

        // Parsers
        bool ParseReflect(int isInverted)
        {
            if (isInverted == 0)
            {
                return false;
            }

            return true;
        }

        string ParseSourceComponentType(int type)
        {
            switch (type)
            {
                case 0:
                    return "X";
                case 1:
                    return "Y";
                case 2:
                    return "Angle";
                default:
                    return "X";
            }
        }
        #endregion
        
        # region Inputs
        JArray GetInputs(JArray unconvertedJArray)
        {
            JArray convertedJArray = new JArray();
            foreach (var token in unconvertedJArray)
            {
                var obj = (token as JObject).GetValue("0 CubismPhysicsInput data") as JObject;
                convertedJArray.Add(GenerateInputObject(obj));
                inputCount++;
            }
            return convertedJArray;
        }

        JToken GenerateInputObject(JObject unconvertedInputObj)
        {
            JObject sourceObj = new JObject
            {
                {"Target", "Parameter"}, {"Id", unconvertedInputObj.GetValue("1 string SourceId")}
            };
            JObject convertedInputObj = new JObject
            {
                {
                    "Source",
                    sourceObj
                },
                {
                    "Weight",
                    unconvertedInputObj.GetValue("0 float Weight")
                },
                {
                    "Type",
                    ParseSourceComponentType((int)unconvertedInputObj.GetValue("0 int SourceComponent"))
                },
                {
                    "Reflect",
                    ParseReflect((int)unconvertedInputObj.GetValue("1 UInt8 IsInverted"))
                }

            };
            return convertedInputObj;
        }
        #endregion
        
        #region Outputs
        JArray GetOutputs(JArray unconvertedJArray)
        {
            JArray convertedJArray = new JArray();
            foreach (var token in unconvertedJArray)
            {
                var obj = (token as JObject).GetValue("0 CubismPhysicsOutput data") as JObject;
                convertedJArray.Add(GenerateOutputObject(obj));
                outputCount++;
            }
            return convertedJArray;
        }

        JToken GenerateOutputObject(JObject unconvertedOutputObj)
        {
            JObject dstObj = new JObject
            {
                {"Target", "Parameter"}, {"Id", unconvertedOutputObj.GetValue("1 string DestinationId")}
            };
            JObject convertedOutputObject = new JObject
            {
                {
                    "Destination",
                    dstObj
                },
                {
                    "VertexIndex",
                    unconvertedOutputObj.GetValue("0 int ParticleIndex")
                },
                {
                    "Scale",
                    unconvertedOutputObj.GetValue("0 float AngleScale")
                },
                {
                    "Weight",
                    unconvertedOutputObj.GetValue("0 float Weight")
                },
                {
                    "Type",
                    ParseSourceComponentType((int) unconvertedOutputObj.GetValue("0 int SourceComponent"))
                },
                {
                    "Reflect",
                    ParseReflect((int) unconvertedOutputObj.GetValue("1 UInt8 IsInverted"))
                }
            };
            return convertedOutputObject;
        }

        #endregion

        #region Vertex
        
        JArray GetVertices(JArray unconvertedJArray)
        {
            JArray convertedJArray = new JArray();
            foreach (var token in unconvertedJArray)
            {
                var obj = (token as JObject).GetValue("0 CubismPhysicsParticle data") as JObject;
                convertedJArray.Add(GenerateVertexObject(obj));
                vertexCount++;
            }
            return convertedJArray;
        }

        JToken GenerateVertexObject(JObject unconvertedVertObj)
        {
            JObject iniPosVector = unconvertedVertObj.GetValue("0 Vector2f InitialPosition") as JObject;
            JObject positionObj = new JObject
            {
                {"X", iniPosVector.GetValue("0 float x")}, {"Y", iniPosVector.GetValue("0 float y")}
            };
            JObject convertedVertexObject = new JObject
            {
                {
                    "Position",
                    positionObj
                },
                {
                    "Mobility",
                    unconvertedVertObj.GetValue("0 float Mobility")
                },
                {
                    "Delay",
                    unconvertedVertObj.GetValue("0 float Delay")
                },
                {
                    "Acceleration",
                    unconvertedVertObj.GetValue("0 float Acceleration")
                },
                {
                    "Radius",
                    unconvertedVertObj.GetValue("0 float Radius")
                }
            };
            return convertedVertexObject;
        }

        #endregion

        #region Normalization

        JObject GetNormalization(JObject unconvertedNormalizationObj)
        {
            JObject posTuple = unconvertedNormalizationObj.GetValue("0 CubismPhysicsNormalizationTuplet Position") as JObject;
            JObject angTuple = unconvertedNormalizationObj.GetValue("0 CubismPhysicsNormalizationTuplet Angle") as JObject;
            JObject positionObj = new JObject
            {
                {
                    "Minimum",
                    posTuple.GetValue("0 float Minimum")
                },
                {
                    "Default",
                    posTuple.GetValue("0 float Default")
                },
                {
                    "Maximum",
                    posTuple.GetValue("0 float Maximum")
                }
            };
            JObject angleObj = new JObject
            {
                {
                    "Minimum",
                    angTuple.GetValue("0 float Minimum")
                },
                {
                    "Default",
                    angTuple.GetValue("0 float Default")
                },
                {
                    "Maximum",
                    angTuple.GetValue("0 float Maximum")
                }
            };
            JObject convertedNormalizationObject = new JObject
            {
                {
                    "Position",
                    positionObj
                },
                {
                    "Angle",
                    angleObj
                }
            };
            return convertedNormalizationObject;
        }

        #endregion
        #endregion
    }
}