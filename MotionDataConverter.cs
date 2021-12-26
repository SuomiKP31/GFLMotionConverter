using System;
using Newtonsoft.Json.Linq;


namespace MotionConverter
{
    public class MotionDataConverter
    {
        // Added Functionality:
        // Count points and segments, as well as get the duration
        
        // THESE ARE META PARAMS, ONE FOR EACH ACTION3 FILE
        // Curves = number of curves
        // Points = 3 * B(Bezier segment, each has 3 control points)+ 1 * (I,S,L Segments) + CurveCount(First Point of each Curve)
        // Segments = total amount of segments in all curves
        // Duration = maximum t we get during processing
        JObject inputObject; // should be readonly
        int curveCount;
        int segmentCount;
        int pointCount;
        float duration;
        public MotionDataConverter()
        {
            
        }

        public JObject Convert(JObject cubismFadeMotionData)
        {
            //Initialize
            JObject result = new JObject();
            inputObject = (JObject) cubismFadeMotionData.GetValue("0 MonoBehaviour Base");
            curveCount = 0;
            segmentCount = 0;
            pointCount = 0;
            duration = 0;
            
            // Process Data (MetaData will be obtained)
            var processedData = GetCurves(inputObject);
            
            // Write to JObject
            WriteHead(result);
            WriteCurves(result, processedData);
            return result;
        }

        class KeyFrame
        {
            public float time, value, inSlope, outSlope;
        }
        void WriteHead(JObject dstData)
        {
            dstData.Add("Version", 3);
            JObject meta = new JObject();
            meta.Add("Duration", duration);
            meta.Add("Fps", 30.0f);
            meta.Add("Loop", true);
            meta.Add("AreBeziersRestricted", true);
            meta.Add("CurveCount", curveCount);
            meta.Add("TotalSegmentCount", segmentCount);
            meta.Add("TotalPointCount", pointCount + curveCount); // REMEMBER THE FIRST POINTS!
            meta.Add("UserDataCount", 0);
            meta.Add("TotalUserDataSize", 0);
            dstData.Add("Meta", meta);
        }
        
        void WriteCurves(JObject dstData, JArray curves)
        {
            
            dstData.Add("Curves", curves);
        }

        JArray GetCurves(JObject srcData)
        {
            JArray curves = new JArray();
            string[] parameterIds = GetParameterIds(srcData);
            float[] parameterFadeInTimes = GetParameterFadeInTimes(srcData);
            float[] parameterFadeOutTimes = GetParameterFadeOutTimes(srcData);

            JArray animationCurves = (JArray) ((JObject) srcData.GetValue("0 vector ParameterCurves")).GetValue("1 Array Array");
            for (int i = 0; i < parameterIds.Length; i++)
            {
                if (string.IsNullOrEmpty(parameterIds[i]))
                {
                    continue;
                }

                JObject curve = new JObject();
                curve.Add("Target", "Parameter");
                curve.Add("Id",parameterIds[i]);
                if(parameterFadeInTimes[i] >= 0.0f)
                    curve.Add("FadeInTime", parameterFadeInTimes[i]);
                if (parameterFadeOutTimes[i] >= 0.0f)
                    curve.Add("FadeOutTime", parameterFadeOutTimes[i]);

                curve.Add("Segments",
                    ConvertKeyFramesToCurveSegments(
                        (JObject) ((JObject) animationCurves[i]).GetValue("0 AnimationCurve data")));
                curves.Add(curve);
            }

            curveCount = curves.Count;
            return curves;
        }

        JArray ConvertKeyFramesToCurveSegments(JObject animationCurve)
        {
            JArray result = new JArray();
            JArray curveArray =
                (JArray) ((JObject) animationCurve.GetValue("0 vector m_Curve")).GetValue("1 Array Array");
            if (curveArray.Count == 0)
            {
                return result;
            }

            KeyFrame[] keyframes = ConvertJsonToArray(curveArray);
            
            result.Add(keyframes[0].time);
            result.Add(keyframes[0].value);

            duration = keyframes[0].time;
            float timeOfFrame = 0;
            
            for (int j = 1; j < keyframes.Length; j++)
            {
                
                // Keyframe Type
                if (j + 1 < keyframes.Length && keyframes[j].inSlope != 0 && keyframes[j].outSlope == 0.0f 
                    && keyframes[j+1].inSlope == 0.0f && keyframes[j+1].outSlope == 0.0f)
                {
                    result.Add(3); // TYPE: InverseStepped
                    result.Add(keyframes[j+1].time);
                    result.Add(keyframes[j+1].value);
                    
                    pointCount += 1;
                    segmentCount += 1;
                    timeOfFrame = keyframes[j + 1].time;
                }

                else if (float.IsPositiveInfinity(keyframes[j].inSlope))
                {
                    result.Add(2); // TYPE: Stepped
                    result.Add(keyframes[j].time);
                    result.Add(keyframes[j].value);
                    
                    pointCount += 1;
                    segmentCount += 1;
                    timeOfFrame = keyframes[j].time;
                }
                
                else if (keyframes[j - 1].outSlope == keyframes[j].inSlope)
                {
                    result.Add(0); // TYPE: Linear
                    result.Add(keyframes[j].time);
                    result.Add(keyframes[j].value);
                    
                    pointCount += 1;
                    segmentCount += 1;
                    timeOfFrame = keyframes[j].time;
                }
                else
                {
                    result.Add(1); // TYPE: Bezier
                    float tangentLength = Math.Abs(keyframes[j - 1].time - keyframes[j].time) * 0.3333333f;
                    result.Add(0);
                    result.Add(keyframes[j-1].outSlope * tangentLength + keyframes[j-1].value);
                    result.Add(0);
                    result.Add(keyframes[j].value - keyframes[j].inSlope * tangentLength);
                    result.Add(keyframes[j].time);
                    result.Add(keyframes[j].value);
                    
                    pointCount += 3;
                    segmentCount += 1;
                    timeOfFrame = keyframes[j].time;
                }

                
            }
            duration = MathF.Max(duration, timeOfFrame); // Duration is the max timestamp among all keyframes
            // It seems all curves has its last frame's timestamp at the end so I think it's safe to put it outside the loop
            return result;
        }

        KeyFrame[] ConvertJsonToArray(JArray array)
        {
            KeyFrame[] res = new KeyFrame[array.Count];
            for (int i = 0; i < array.Count; i++)
            {
                JObject obj = (JObject)((JObject) array[i]).GetValue("0 Keyframe data");
                res[i] = new KeyFrame();
                ref var kf = ref res[i];
                // the inslope value could be a string, as we treated 1.#INF as a string in preprocess state, in Program.cs line 43
                // Here, we convert it to PositiveInfinite if that's the case
                
                kf.time = (float)obj.GetValue("0 float time");
                kf.value = (float) obj.GetValue("0 float value");
                //kf.inSlope = (float) obj.GetValue("0 float inSlope");
                kf.outSlope = (float) obj.GetValue("0 float outSlope");

                if ((string)obj.GetValue("0 float inSlope") == "1.#INF")
                {
                    kf.inSlope = Single.PositiveInfinity;
                }
                else
                {
                    kf.inSlope = (float) obj.GetValue("0 float inSlope");
                }

            }

            return res;
        }
        
        string[] GetParameterIds(JObject srcData)
        {
            JArray array = (JArray)((JObject) srcData.GetValue("0 vector ParameterIds")).GetValue("1 Array Array");
            string[] result = new string[array.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (string) ((JObject) array[i]).GetValue("1 string data");
            }

            return result;
        }

        float[] GetParameterFadeOutTimes(JObject srcData)
        {
            JArray array = (JArray)((JObject) srcData.GetValue("0 vector ParameterFadeOutTimes")).GetValue("1 Array Array");
            float[] result = new float[array.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (float) ((JObject) array[i]).GetValue("0 float data");
            }

            return result;
        }

        float[] GetParameterFadeInTimes(JObject srcData)
        {
            JArray array = (JArray)((JObject) srcData.GetValue("0 vector ParameterFadeInTimes")).GetValue("1 Array Array");
            float[] result = new float[array.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (float) ((JObject) array[i]).GetValue("0 float data");
            }

            return result;
        }

        
    }
}