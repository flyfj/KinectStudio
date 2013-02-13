using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows;
using System.Web;
using Microsoft.Kinect;
using System.Xml;
using System.Diagnostics;


namespace KinectMotionAnalyzer.Processors
{

    /// <summary>
    /// user gesture
    /// </summary>
    public class Gesture
    {
        // actual gesture data
        public List<Skeleton> data = new List<Skeleton>();
        public string name = "Unknown";
    }

    /// <summary>
    /// common data for all template gesture
    /// </summary>
    public class GestureTemplateBase
    {
        // identity
        public string name = "Unknown";
        public int id = -1;

        // weight for each joint used in recognition (matching, currently only binary)
        public Dictionary<JointType, float> jointWeights = new Dictionary<JointType, float>();

        public GestureTemplateBase()
        {
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
    /// recognizer based on dtw
    /// </summary>
    class GestureRecognizer
    {

        private string GESTURE_DATABASE_DIR = "D:\\gdata\\";

        // dynamically generate
        public Dictionary<int, string> GESTURE_LIST = new Dictionary<int, string>();

        // configuration for each database gesture type
        public Dictionary<int, GestureTemplateBase> GESTURE_CONFIG = 
            new Dictionary<int, GestureTemplateBase>();

        // database gesture data
        private Dictionary<int, List<Gesture>> GESTURE_DATABASE = 
            new Dictionary<int, List<Gesture>>();

        // maximum and minimum gesture length in database: used to define a valid test gesture
        public int gesture_min_len = 0;
        public int gesture_max_len = 0;


        public GestureRecognizer()
        {
            // create database dir if not exist yet
            if (!Directory.Exists(GESTURE_DATABASE_DIR))
                Directory.CreateDirectory(GESTURE_DATABASE_DIR);
        }


        /// <summary>
        /// gesture config management
        /// </summary>
        public bool AddGestureConfig(GestureTemplateBase gbase)
        {
            string gdir = GESTURE_DATABASE_DIR + gbase.name;

            // save config file
            string gfilename = GESTURE_DATABASE_DIR + gbase.name + ".xml";
            SaveGestureConfig(gbase);

            // create new directory
            if (!Directory.Exists(gdir))
                Directory.CreateDirectory(gdir);

            int max_id = (GESTURE_LIST.Keys.Count > 0 ? GESTURE_LIST.Keys.Max() : -1);
            gbase.id = max_id + 1;
            GESTURE_LIST.Add(max_id+1, gbase.name);
            GESTURE_CONFIG.Add(max_id + 1, gbase);

            return true;
        }

        public bool RemoveGestureConfig(string gname)
        {
            if (!GESTURE_LIST.ContainsValue(gname))
                return false;

            // remove from data structure
            int gid = GESTURE_LIST.FirstOrDefault(x => x.Value == gname).Key;
            GESTURE_CONFIG.Remove(gid);
            GESTURE_LIST.Remove(gid);

            // delete config file
            string filename = GESTURE_DATABASE_DIR + gname + ".xml";
            if (File.Exists(filename))
                File.Delete(filename);

            // delete directory and all data
            string gdir = GESTURE_DATABASE_DIR + gname;
            if (Directory.Exists(gdir))
                Directory.Delete(gdir, true);

            return true;
        }

        public bool SaveGestureConfig(GestureTemplateBase config)
        {
            
            // write config file for each gesture model
            // format: <Gesture name = "">
            //              <Joint type="" weight=""></Joint>
            //         </Gesture>
            string filename = GESTURE_DATABASE_DIR + config.name + ".xml";

            XmlDocument xmldoc = new XmlDocument();
            XmlDeclaration declar = xmldoc.CreateXmlDeclaration("1.0", null, null);
            xmldoc.AppendChild(declar);
            // create root element
            XmlElement root = xmldoc.CreateElement("Gesture");
            root.SetAttribute("name", config.name);
            xmldoc.AppendChild(root);

            #region output_joint_weights

            // add joints
            foreach (JointType joint_type in config.jointWeights.Keys)
            {
                XmlElement joint_elem = xmldoc.CreateElement("Joint");
                root.AppendChild(joint_elem);

                joint_elem.SetAttribute("type", joint_type.ToString());
                int jtype = (int)joint_type;
                joint_elem.SetAttribute("typeid", jtype.ToString());
                joint_elem.SetAttribute("weight", config.jointWeights[joint_type].ToString());
            }

            #endregion

            // save to disk
            xmldoc.Save(filename);

            return true;
        }

        public GestureTemplateBase LoadGestureConfig(string filename, int gid)
        {
            if (!File.Exists(filename))
                return null;

            GestureTemplateBase basis = new GestureTemplateBase();

            XmlDocument doc = new XmlDocument();
            doc.Load(filename);

            XmlElement root = doc.DocumentElement;
            basis.name = root.Attributes["name"].Value;
            basis.id = gid;
            for (int i = 0; i < root.ChildNodes.Count; i++)
            {
                XmlElement joint_elem = root.ChildNodes[i] as XmlElement;
                int jtype = int.Parse(joint_elem.Attributes["typeid"].Value);
                JointType type = (JointType)jtype;
                float weight = float.Parse(joint_elem.Attributes["weight"].Value);
                basis.jointWeights[type] = weight;
            }

            return basis;
        }

        public bool LoadAllGestureConfig()
        {
            if (!Directory.Exists(GESTURE_DATABASE_DIR))
                return false;

            GESTURE_LIST.Clear();
            GESTURE_CONFIG.Clear();

            // look for config xml file under database root directory: XXX.xml
            IEnumerable<string> gesture_config_files = Directory.EnumerateFiles(GESTURE_DATABASE_DIR, "*.xml");
            int gid = 0;
            foreach (string g_cfile in gesture_config_files)
            {
                // get gesture name
                int slash_id = g_cfile.LastIndexOf('\\');
                string gesture_name = g_cfile.Substring(slash_id + 1, g_cfile.Length - slash_id - 5);

                // add to list
                GESTURE_LIST.Add(gid, gesture_name);

                // read configuration file
                GestureTemplateBase cur_basis = LoadGestureConfig(g_cfile, gid);

                GESTURE_CONFIG.Add(gid, cur_basis);

                gid++;
            }

            return true;
        }


        /// <summary>
        /// read database data from files
        /// </summary>
        public bool LoadGestureDatabase(string database_dir)
        {
            // enumerate gesture directories
            if (!Directory.Exists(database_dir))
                return false;

            // clear
            GESTURE_LIST.Clear();
            GESTURE_CONFIG.Clear();
            GESTURE_DATABASE.Clear();

            // load all gesture config
            if (!LoadAllGestureConfig())
            {
                Debug.WriteLine("Fail to load gesture config.");
                return false;
            }

            // load actual gesture data for each type
            foreach (int gid in GESTURE_LIST.Keys)
            {
                string gdir = GESTURE_DATABASE_DIR + GESTURE_LIST[gid] + "\\";
                List<Gesture> cur_gestures = new List<Gesture>();
                IEnumerable<string> gesture_files = Directory.EnumerateFiles(gdir, "*.xml");
                foreach (string filename in gesture_files)
                {
                    Gesture gtemp = new Gesture();
                    gtemp.data = KinectRecorder.ReadFromSkeletonFile(filename);
                    gtemp.name = GESTURE_LIST[gid];

                    if (gtemp.data.Count > gesture_max_len)
                        gesture_max_len = gtemp.data.Count;
                    if (gtemp.data.Count < gesture_min_len)
                        gesture_min_len = gtemp.data.Count;

                    cur_gestures.Add(gtemp);
                }

                // add to database
                if (cur_gestures.Count > 0)
                    GESTURE_DATABASE.Add(gid, cur_gestures);
            }

            // no actual model data
            if (GESTURE_DATABASE.Count == 0)
                return false;

            return true;
        }

        /// <summary>
        /// preprocess input data and transform to 1d feature vector for DTW 
        /// according to specific gesture model
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private ArrayList PreprocessGesture(Gesture input, int gid)
        {
            ArrayList dtw_data = new ArrayList();
            // each frame
            for(int i=0; i<input.data.Count; i++)
            {
                // Extract the coordinates of the points.
                List<Point> pts = new List<Point>();
                Point shoulderRight = new Point(
                    input.data[i].Joints[JointType.ShoulderRight].Position.X,
                    input.data[i].Joints[JointType.ShoulderRight].Position.Y);
                Point shoulderLeft = new Point(
                    input.data[i].Joints[JointType.ShoulderLeft].Position.X,
                    input.data[i].Joints[JointType.ShoulderLeft].Position.Y);

                foreach (Joint j in input.data[i].Joints)
                {
                    if (GESTURE_CONFIG[gid].jointWeights[j.JointType] > 0)
                    {
                        Point p = new Point(j.Position.X, j.Position.Y);
                        pts.Add(p);
                    }
                }

                // Center the data
                var center = new Point(
                    input.data[i].Joints[JointType.ShoulderCenter].Position.X, 
                    input.data[i].Joints[JointType.ShoulderCenter].Position.Y);
                for (int k = 0; k < pts.Count; k++)
                    pts[k] = new Point(pts[k].X - center.X, pts[k].Y - center.Y);

                // Normalization of the coordinates
                double shoulderDist =
                    Math.Sqrt(Math.Pow((shoulderLeft.X - shoulderRight.X), 2) +
                              Math.Pow((shoulderLeft.Y - shoulderRight.Y), 2));
                for (int k = 0; k < pts.Count; k++)
                    pts[k] = new Point(pts[k].X / shoulderDist, pts[k].Y / shoulderDist);

                // save in 1d double array
                double[] feat_vec = new double[pts.Count * 2];
                for (int k = 0; k < pts.Count; k++)
                {
                    feat_vec[k * 2] = pts[k].X;
                    feat_vec[k * 2 + 1] = pts[k].Y;
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
            res = "Unknown";
            foreach (int gid in GESTURE_DATABASE.Keys)
            {
                foreach (Gesture temp in GESTURE_DATABASE[gid])
                {
                    double dist = GestureSimilarity(input, temp, gid);
                    if(dist < mindist)
                    {
                        mindist = dist;
                        res = GESTURE_LIST[gid];
                    }
                }
            }

            return mindist;
        }


        /// <summary>
        /// measure similarity between input gesture and a gesture template
        /// </summary>
        public double GestureSimilarity(Gesture input, Gesture template, int gid)
        {
            ArrayList input_data = PreprocessGesture(input, gid);
            ArrayList temp_data = PreprocessGesture(template, gid);

            double dist = DynamicTimeWarping(input_data, temp_data, GESTURE_CONFIG[gid].jointWeights);

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
