using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows;
using System.Web;
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

        public GestureTemplateBase basis;
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
        /// preprocess input data and transform to 1d feature vector for DTW
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public ArrayList PreprocessGesture(Gesture input)
        {
            ArrayList dtw_data = new ArrayList();
            for(int i=0; i<input.data.Count; i++)
            {
                // Extract the coordinates of the points.
                var p = new Point[6];
                Point shoulderRight = new Point(), shoulderLeft = new Point();
                foreach (Joint j in input.data[i].Joints)
                {
                    switch (j.JointType)
                    {
                        case JointType.HandLeft:
                            p[0] = new Point(j.Position.X, j.Position.Y);
                            break;
                        case JointType.WristLeft:
                            p[1] = new Point(j.Position.X, j.Position.Y);
                            break;
                        case JointType.ElbowLeft:
                            p[2] = new Point(j.Position.X, j.Position.Y);
                            break;
                        case JointType.ElbowRight:
                            p[3] = new Point(j.Position.X, j.Position.Y);
                            break;
                        case JointType.WristRight:
                            p[4] = new Point(j.Position.X, j.Position.Y);
                            break;
                        case JointType.HandRight:
                            p[5] = new Point(j.Position.X, j.Position.Y);
                            break;
                        case JointType.ShoulderLeft:
                            shoulderLeft = new Point(j.Position.X, j.Position.Y);
                            break;
                        case JointType.ShoulderRight:
                            shoulderRight = new Point(j.Position.X, j.Position.Y);
                            break;
                    }
                }

                // Center the data
                var center = new Point((shoulderLeft.X + shoulderRight.X) / 2, (shoulderLeft.Y + shoulderRight.Y) / 2);
                for (int k = 0; k < p.Length; k++)
                {
                    p[k].X -= center.X;
                    p[k].Y -= center.Y;
                }

                // Normalization of the coordinates
                double shoulderDist =
                    Math.Sqrt(Math.Pow((shoulderLeft.X - shoulderRight.X), 2) +
                              Math.Pow((shoulderLeft.Y - shoulderRight.Y), 2));
                for (int k = 0; k < p.Length; k++)
                {
                    p[k].X /= shoulderDist;
                    p[k].Y /= shoulderDist;
                }

                // save in 1d double array
                double[] feat_vec = new double[p.Length * 2];
                for (int k = 0; k < p.Length; k++)
                {
                    feat_vec[k * 2] = p[k].X;
                    feat_vec[k * 2 + 1] = p[k].Y;
                }

                dtw_data.Add(feat_vec);

            }

            return dtw_data;
        }


        public ArrayList PreprocessGesture(GestureTemplate input)
        {
            ArrayList dtw_data = new ArrayList();
            for (int i = 0; i < input.data.Count; i++)
            {
                // Extract the coordinates of the points.
                var p = new Point[6];
                Point shoulderRight = new Point(), shoulderLeft = new Point();
                foreach (Joint j in input.data[i].Joints)
                {
                    switch (j.JointType)
                    {
                        case JointType.HandLeft:
                            p[0] = new Point(j.Position.X, j.Position.Y);
                            break;
                        case JointType.WristLeft:
                            p[1] = new Point(j.Position.X, j.Position.Y);
                            break;
                        case JointType.ElbowLeft:
                            p[2] = new Point(j.Position.X, j.Position.Y);
                            break;
                        case JointType.ElbowRight:
                            p[3] = new Point(j.Position.X, j.Position.Y);
                            break;
                        case JointType.WristRight:
                            p[4] = new Point(j.Position.X, j.Position.Y);
                            break;
                        case JointType.HandRight:
                            p[5] = new Point(j.Position.X, j.Position.Y);
                            break;
                        case JointType.ShoulderLeft:
                            shoulderLeft = new Point(j.Position.X, j.Position.Y);
                            break;
                        case JointType.ShoulderRight:
                            shoulderRight = new Point(j.Position.X, j.Position.Y);
                            break;
                    }
                }

                // Center the data
                var center = new Point((shoulderLeft.X + shoulderRight.X) / 2, (shoulderLeft.Y + shoulderRight.Y) / 2);
                for (int k = 0; k < 6; k++)
                {
                    p[k].X -= center.X;
                    p[k].Y -= center.Y;
                }

                // Normalization of the coordinates
                double shoulderDist =
                    Math.Sqrt(Math.Pow((shoulderLeft.X - shoulderRight.X), 2) +
                              Math.Pow((shoulderLeft.Y - shoulderRight.Y), 2));
                for (int k = 0; k < 6; k++)
                {
                    p[k].X /= shoulderDist;
                    p[k].Y /= shoulderDist;
                }

                // save in 1d double array
                double[] feat_vec = new double[p.Length * 2];
                for (int k = 0; k < p.Length; k++)
                {
                    feat_vec[k * 2] = p[k].X;
                    feat_vec[k * 2 + 1] = p[k].Y;
                }

                dtw_data.Add(feat_vec);

            }

            return dtw_data;

        }

        /// <summary>
        /// match to each database template
        /// </summary>
        public double MatchToDatabase(Gesture input, out string res)
        {

            // find the most similar gesture in database to test gesture
            double mindist = double.PositiveInfinity;
            GestureName bestname = GestureName.Unknown;
            foreach (GestureName gname in GESTURE_DATABASE.Keys)
            {
                foreach (GestureTemplate temp in GESTURE_DATABASE[gname])
                {
                    double dist = GestureSimilarity(input, temp);
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
        public double GestureSimilarity(Gesture input, GestureTemplate template)
        {
            ArrayList input_data = PreprocessGesture(input);
            ArrayList temp_data = PreprocessGesture(template);

            double dist = DynamicTimeWarping(input_data, temp_data, template.basis.jointWeights);/// temp_data.Count;

            return dist;
        }


        /// <summary>
        /// generic dtw algorithm
        /// bug: as long as they have similar finishing pose, it will be detected
        /// </summary>
        public double DynamicTimeWarping(
            ArrayList input1, ArrayList input2, Dictionary<JointType, float> weights)
        {
            if (input1 == null || input2 == null || input1.Count == 0 || input2.Count == 0)
                return -1;

            // perform DTW to align two arrays
            int length1 = input1.Count;
            int length2 = input2.Count;
            double[,] DTW = new double[length1+1, length2+1];   // make an extra space for 0 match

            for (int i = 1; i <= length1; i++)
                DTW[i, 0] = double.PositiveInfinity;
            for (int j = 1; j <= length2; j++)
                DTW[0, j] = double.PositiveInfinity;
            DTW[0, 0] = 0;

            for(int i=1; i<=length1; i++)
            {
                for(int j=1; j<=length2; j++)
                {
                    double cost = Tools.Dist2((double[])input1[i - 1], (double[])input2[j - 1]);
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
