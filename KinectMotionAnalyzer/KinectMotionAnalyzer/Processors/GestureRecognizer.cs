using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Kinect;


namespace KinectMotionAnalyzer.Processors
{

    enum GestureName
    {
        Unknown,
        Bicep_Curl,
        Squat,
        Shoulder_Press
    }

    /// <summary>
    /// user gesture
    /// </summary>
    class Gesture
    {
        // actual gesture data
        public List<Skeleton> data = new List<Skeleton>();
        public GestureName name = GestureName.Unknown;
    }

    /// <summary>
    /// common data for all template gesture
    /// </summary>
    class GestureTemplateBase
    {
        public GestureName name = GestureName.Unknown;
        // weight for each joint used in recognition (matching)
        public Dictionary<JointType, float> jointWeights = new Dictionary<JointType, float>();

        public GestureTemplateBase()
        {
            name = GestureName.Unknown;
            jointWeights[JointType.AnkleLeft] = 0;
            jointWeights[JointType.AnkleRight] = 0;
            jointWeights[JointType.ElbowLeft] = 0;
            jointWeights[JointType.ElbowRight] = 0;
            jointWeights[JointType.FootLeft] = 0;
            jointWeights[JointType.FootRight] = 0;
            jointWeights[JointType.HandLeft] = 0;
            jointWeights[JointType.HandRight] = 0;
            jointWeights[JointType.Head] = 0;
            jointWeights[JointType.HipCenter] = 0;
            jointWeights[JointType.HipLeft] = 0;
            jointWeights[JointType.HipRight] = 0;
            jointWeights[JointType.KneeLeft] = 0;
            jointWeights[JointType.KneeRight] = 0;
            jointWeights[JointType.ShoulderCenter] = 0;
            jointWeights[JointType.ShoulderLeft] = 0;
            jointWeights[JointType.ShoulderRight] = 0;
            jointWeights[JointType.Spine] = 0;
            jointWeights[JointType.WristLeft] = 0;
            jointWeights[JointType.WristRight] = 0;
        }

    }

    /// <summary>
    /// template gesture
    /// </summary>
    class GestureTemplate
    {
        // actual gesture data
        public List<Skeleton> data = new List<Skeleton>();

        public GestureTemplateBase basis = new GestureTemplateBase();
    }


    /// <summary>
    /// recognizer based on dtw
    /// </summary>
    class GestureRecognizer
    {

        private Dictionary<string, GestureTemplateBase> GESTURE_BASES = 
            new Dictionary<string, GestureTemplateBase>();

        private Dictionary<GestureName, List<GestureTemplate>> GESTURE_DATABASE = 
            new Dictionary<GestureName, List<GestureTemplate>>();

        public int gesture_min_len = 0;
        public int gesture_max_len = 0;

        public GestureRecognizer()
        {
            // set up gesture type mapping
            GestureTemplateBase unknown_basis = new GestureTemplateBase();
            GESTURE_BASES.Add("Unknown", unknown_basis);
            
            GestureTemplateBase bicep_curl_basis = new GestureTemplateBase();
            bicep_curl_basis.name = GestureName.Bicep_Curl;
            bicep_curl_basis.jointWeights[JointType.ShoulderLeft] = 1;
            bicep_curl_basis.jointWeights[JointType.ShoulderRight] = 1;
            bicep_curl_basis.jointWeights[JointType.ElbowLeft] = 1;
            bicep_curl_basis.jointWeights[JointType.ElbowRight] = 1;
            GESTURE_BASES.Add("Bicep_Curl", bicep_curl_basis);

            GestureTemplateBase squat_basis = new GestureTemplateBase();
            squat_basis.name = GestureName.Squat;
            GESTURE_BASES.Add("Squat", squat_basis);

            GestureTemplateBase shoulder_press_basis = new GestureTemplateBase();
            shoulder_press_basis.name = GestureName.Shoulder_Press;
            GESTURE_BASES.Add("Shoulder_Press", shoulder_press_basis);
        }


        /// <summary>
        /// read database data from files
        /// </summary>
        public bool LoadGestureDatabase(string database_dir)
        {
            // enumerate gesture directories
            if (!Directory.Exists(database_dir))
                return false;

            GESTURE_DATABASE.Clear();

            IEnumerable<string> gesture_dirs = Directory.EnumerateDirectories(database_dir);
            gesture_min_len = int.MaxValue;
            gesture_max_len = int.MinValue;
            foreach (string gdir in gesture_dirs)
            {
                // get dir name
                int slash_id = gdir.LastIndexOf('\\');
                string dirname = gdir.Substring(slash_id+1, gdir.Length - slash_id-1);
                if (!GESTURE_BASES.ContainsKey(dirname))
                    continue;

                List<GestureTemplate> cur_gestures = new List<GestureTemplate>();
                IEnumerable<string> gesture_files = Directory.EnumerateFiles(gdir, "*.xml");
                foreach (string filename in gesture_files)
                {
                    GestureTemplate gtemp = new GestureTemplate();
                    gtemp.data = KinectRecorder.ReadFromSkeletonFile(filename);
                    gtemp.basis.name = GESTURE_BASES[dirname].name;

                    if (gtemp.data.Count > gesture_max_len)
                        gesture_max_len = gtemp.data.Count;
                    if (gtemp.data.Count < gesture_min_len)
                        gesture_min_len = gtemp.data.Count;

                    cur_gestures.Add(gtemp);
                }

                if(cur_gestures.Count > 0)
                {
                    // add to database
                    GESTURE_DATABASE.Add(GESTURE_BASES[dirname].name, cur_gestures);
                }
                
            }

            if (GESTURE_DATABASE.Count == 0)
                return false;

            return true;
        }


        /// <summary>
        /// match to each database template
        /// </summary>
        public float MatchToDatabase(Gesture input, out string res)
        {
            // find the most similar gesture in database to test gesture
            float mindist = float.PositiveInfinity;
            GestureName bestname = GestureName.Unknown;
            foreach (GestureName gname in GESTURE_DATABASE.Keys)
            {
                foreach (GestureTemplate temp in GESTURE_DATABASE[gname])
                {
                    float dist = GestureSimilarity(input, temp);
                    if(dist < mindist)
                    {
                        mindist = dist;
                        bestname = gname;
                    }
                }
            }

            res = bestname.ToString();

            return mindist;
        }


        /// <summary>
        /// measure similarity between input gesture and a gesture template
        /// </summary>
        public float GestureSimilarity(Gesture input, GestureTemplate template)
        {
            Gesture input2 = new Gesture();
            GestureTemplate temp2 = new GestureTemplate();
            for (int i = 0; i < input.data.Count; i++)
            {
                input2.data.Add(input.data[i]);
                input2.data[i] = NormalizeSkeleton(input2.data[i]);
            }

            for (int i = 0; i < template.data.Count; i++)
            {
                temp2.data.Add(template.data[i]);
                temp2.data[i] = NormalizeSkeleton(temp2.data[i]);
            }

            float dist = DynamicTimeWarping(input2.data, temp2.data, template.basis.jointWeights);

            return dist;
        }

        private Skeleton NormalizeSkeleton(Skeleton input)
        {
            // normalize
            // subtract each joint position with center position
            // normalize size with shoulder width
            SkeletonPoint centerpt = input.Joints[JointType.HipCenter].Position;
            float shoulderWidth = Tools.GetJointDistance(
                input.Joints[JointType.ShoulderLeft].Position,
                input.Joints[JointType.ShoulderRight].Position);

            foreach (Joint joint in input.Joints)
            {
                SkeletonPoint point = new SkeletonPoint();
                Joint tjoint = joint;
                // normalize position
                point.X = joint.Position.X - centerpt.X;
                point.Y = joint.Position.Y - centerpt.Y;
                point.Z = joint.Position.Z - centerpt.Z;
                // normalize size
                //point.X /= shoulderWidth;
                //point.Y /= shoulderWidth;
                //point.Z /= shoulderWidth;

                tjoint.Position = point;
                input.Joints[joint.JointType] = tjoint;
            }

            return input;
        }


        /// <summary>
        /// generic dtw algorithm
        /// </summary>
        public float DynamicTimeWarping(
            List<Skeleton> input1, List<Skeleton> input2, Dictionary<JointType, float> weights)
        {
            if (input1 == null || input2 == null || input1.Count == 0 || input2.Count == 0)
                return -1;

            // perform DTW to align two arrays
            int length1 = input1.Count;
            int length2 = input2.Count;
            float[,] DTW = new float[length1+1, length2+1];   // make an extra space for 0 match

            for(int i=1; i<=length1; i++)
                DTW[i, 0] = float.PositiveInfinity;
            for(int i=1; i<=length2; i++)
                DTW[0, i] = float.PositiveInfinity;
            DTW[0, 0] = 0;

            for(int i=1; i<=length1; i++)
            {
                for(int j=1; j<=length2; j++)
                {
                    int loc1 = i - 1;
                    int loc2 = j - 1;
                    float cost = DistBetweenPose(input1[loc1], input2[loc2], weights);
                    DTW[i, j] = cost + Math.Min(DTW[i-1, j], Math.Min(DTW[i, j-1], DTW[i-1, j-1]));
                }
            }

            return DTW[length1, length2];
        }

        private float DistBetweenPose(Skeleton pose1, Skeleton pose2, Dictionary<JointType, float> weights)
        {
            if (pose1 == null || pose2 == null)
                return -1;

            float dist = 0;
            float sumw = 0;
            for (int i = 0; i < pose1.Joints.Count; i++)
            {
                JointType type = (JointType)i;
                dist += Tools.GetJointDistance(pose1.Joints[type].Position, pose2.Joints[type].Position);
                //sumw += weights[type];
            }

            //dist /= sumw;

            return dist / pose1.Joints.Count;
        }
    }
}
