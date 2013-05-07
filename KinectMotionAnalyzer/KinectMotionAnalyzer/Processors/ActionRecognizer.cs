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
    public class Action
    {
        // actual gesture data
        public List<Skeleton> data = new List<Skeleton>();
        public string name = "Unknown";
    }

    /// <summary>
    /// common data for all template gesture
    /// </summary>
    public class ActionTemplateBase
    {
        // identity
        public string name = "Unknown";
        public int id = -1;

        // weight for each joint used in recognition (matching, currently only binary)
        public Dictionary<JointType, float> jointWeights = new Dictionary<JointType, float>();

        public ActionTemplateBase()
        {
            foreach (JointType type in Enum.GetValues(typeof(JointType)))
            {
                jointWeights[type] = 1;
            }
        }
    }

    /// <summary>
    /// feedback information
    /// </summary>
    public class Feedback
    {

    };

    /// <summary>
    /// recognizer based on dtw
    /// </summary>
    class ActionRecognizer
    {

        // dynamically generate
        public Dictionary<int, string> ACTION_LIST = new Dictionary<int, string>();

        // configuration for each database action type
        public Dictionary<int, ActionTemplateBase> ACTION_CONFIG = 
            new Dictionary<int, ActionTemplateBase>();

        // database gesture data
        private Dictionary<int, List<Action>> ACTION_DATABASE = 
            new Dictionary<int, List<Action>>();

        // accumulated cost matrix
        private double[,] DTW = null;

        // optimal warping path
        private List<Point> WarpingPath = null;

        // maximum and minimum gesture length in database: used to define a valid test gesture
        public int action_min_len = 0;
        public int action_max_len = 0;


        public ActionRecognizer()
        {
            ACTION_CONFIG.Add(0, new ActionTemplateBase());
        }


        /// <summary>
        /// gesture config management
        /// </summary>
        //public bool AddGestureConfig(ActionTemplateBase gbase)
        //{
        //    string gdir = GESTURE_DATABASE_DIR + gbase.name;

        //    // save config file
        //    string gfilename = GESTURE_DATABASE_DIR + gbase.name + ".xml";
        //    SaveGestureConfig(gbase);

        //    // create new directory
        //    if (!Directory.Exists(gdir))
        //        Directory.CreateDirectory(gdir);

        //    int max_id = (ACTION_LIST.Keys.Count > 0 ? ACTION_LIST.Keys.Max() : -1);
        //    gbase.id = max_id + 1;
        //    ACTION_LIST.Add(max_id+1, gbase.name);
        //    ACTION_CONFIG.Add(max_id + 1, gbase);

        //    return true;
        //}

        //public bool RemoveGestureConfig(string gname)
        //{
        //    if (!ACTION_LIST.ContainsValue(gname))
        //        return false;

        //    // remove from data structure
        //    int gid = ACTION_LIST.FirstOrDefault(x => x.Value == gname).Key;
        //    ACTION_CONFIG.Remove(gid);
        //    ACTION_LIST.Remove(gid);

        //    // delete config file
        //    string filename = GESTURE_DATABASE_DIR + gname + ".xml";
        //    if (File.Exists(filename))
        //        File.Delete(filename);

        //    // delete directory and all data
        //    string gdir = GESTURE_DATABASE_DIR + gname;
        //    if (Directory.Exists(gdir))
        //        Directory.Delete(gdir, true);

        //    return true;
        //}

        //public bool SaveGestureConfig(ActionTemplateBase config)
        //{
            
        //    // write config file for each gesture model
        //    // format: <Gesture name = "">
        //    //              <Joint type="" weight=""></Joint>
        //    //         </Gesture>
        //    string filename = GESTURE_DATABASE_DIR + config.name + ".xml";

        //    XmlDocument xmldoc = new XmlDocument();
        //    XmlDeclaration declar = xmldoc.CreateXmlDeclaration("1.0", null, null);
        //    xmldoc.AppendChild(declar);
        //    // create root element
        //    XmlElement root = xmldoc.CreateElement("Gesture");
        //    root.SetAttribute("name", config.name);
        //    xmldoc.AppendChild(root);

        //    #region output_joint_weights

        //    // add joints
        //    foreach (JointType joint_type in config.jointWeights.Keys)
        //    {
        //        XmlElement joint_elem = xmldoc.CreateElement("Joint");
        //        root.AppendChild(joint_elem);

        //        joint_elem.SetAttribute("type", joint_type.ToString());
        //        int jtype = (int)joint_type;
        //        joint_elem.SetAttribute("typeid", jtype.ToString());
        //        joint_elem.SetAttribute("weight", config.jointWeights[joint_type].ToString());
        //    }

        //    #endregion

        //    // save to disk
        //    xmldoc.Save(filename);

        //    return true;
        //}

        //public ActionTemplateBase LoadGestureConfig(string filename, int gid)
        //{
        //    if (!File.Exists(filename))
        //        return null;

        //    ActionTemplateBase basis = new ActionTemplateBase();

        //    XmlDocument doc = new XmlDocument();
        //    doc.Load(filename);

        //    XmlElement root = doc.DocumentElement;
        //    basis.name = root.Attributes["name"].Value;
        //    basis.id = gid;
        //    for (int i = 0; i < root.ChildNodes.Count; i++)
        //    {
        //        XmlElement joint_elem = root.ChildNodes[i] as XmlElement;
        //        int jtype = int.Parse(joint_elem.Attributes["typeid"].Value);
        //        JointType type = (JointType)jtype;
        //        float weight = float.Parse(joint_elem.Attributes["weight"].Value);
        //        basis.jointWeights[type] = weight;
        //    }

        //    return basis;
        //}

        //public bool LoadAllGestureConfig()
        //{
        //    if (!Directory.Exists(GESTURE_DATABASE_DIR))
        //        return false;

        //    ACTION_LIST.Clear();
        //    ACTION_CONFIG.Clear();

        //    // look for config xml file under database root directory: XXX.xml
        //    IEnumerable<string> gesture_config_files = Directory.EnumerateFiles(GESTURE_DATABASE_DIR, "*.xml");
        //    int gid = 0;
        //    foreach (string g_cfile in gesture_config_files)
        //    {
        //        // get gesture name
        //        int slash_id = g_cfile.LastIndexOf('\\');
        //        string gesture_name = g_cfile.Substring(slash_id + 1, g_cfile.Length - slash_id - 5);

        //        // add to list
        //        ACTION_LIST.Add(gid, gesture_name);

        //        // read configuration file
        //        ActionTemplateBase cur_basis = LoadGestureConfig(g_cfile, gid);

        //        ACTION_CONFIG.Add(gid, cur_basis);

        //        gid++;
        //    }

        //    return true;
        //}


        /// <summary>
        /// read database data from files
        /// </summary>
        //public bool LoadGestureDatabase(string database_dir)
        //{
        //    // enumerate gesture directories
        //    if (!Directory.Exists(database_dir))
        //        return false;

        //    // clear
        //    ACTION_LIST.Clear();
        //    ACTION_CONFIG.Clear();
        //    ACTION_DATABASE.Clear();

        //    // load all gesture config
        //    if (!LoadAllGestureConfig())
        //    {
        //        Debug.WriteLine("Fail to load gesture config.");
        //        return false;
        //    }

        //    // load actual gesture data for each type
        //    foreach (int gid in ACTION_LIST.Keys)
        //    {
        //        string gdir = GESTURE_DATABASE_DIR + ACTION_LIST[gid] + "\\";
        //        List<Action> cur_gestures = new List<Action>();
        //        IEnumerable<string> gesture_files = Directory.EnumerateFiles(gdir, "*.xml");
        //        foreach (string filename in gesture_files)
        //        {
        //            Action gtemp = new Action();
        //            KinectRecorder.ReadFromSkeletonXMLFile(filename, out gtemp.data);
        //            gtemp.name = ACTION_LIST[gid];

        //            if (gtemp.data.Count > action_max_len)
        //                action_max_len = gtemp.data.Count;
        //            if (gtemp.data.Count < action_min_len)
        //                action_min_len = gtemp.data.Count;

        //            cur_gestures.Add(gtemp);
        //        }

        //        // add to database
        //        if (cur_gestures.Count > 0)
        //            ACTION_DATABASE.Add(gid, cur_gestures);
        //    }

        //    // no actual model data
        //    if (ACTION_DATABASE.Count == 0)
        //        return false;

        //    return true;
        //}

        /// <summary>
        /// preprocess input data and transform to 1d feature vector for DTW 
        /// according to specific gesture model
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private ArrayList PreprocessGesture(Action input, int gid)
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
                    //if (ACTION_CONFIG[gid].jointWeights[j.JointType] > 0)
                    //{
                        Point p = new Point(j.Position.X, j.Position.Y);
                        pts.Add(p);
                    //}
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
        public double MatchToDatabase(Action input, out string res)
        {

            // find the most similar gesture in database to test gesture
            double mindist = double.PositiveInfinity;
            res = "Unknown";
            foreach (int gid in ACTION_DATABASE.Keys)
            {
                foreach (Action temp in ACTION_DATABASE[gid])
                {
                    double dist = ActionSimilarity(input, temp, gid);
                    if(dist < mindist)
                    {
                        mindist = dist;
                        res = ACTION_LIST[gid];
                    }
                }
            }

            return mindist;
        }


        /// <summary>
        /// measure similarity between input gesture and a gesture template
        /// </summary>
        public double ActionSimilarity(Action input, Action template, int gid)
        {
            ArrayList input_data = PreprocessGesture(input, gid);
            ArrayList temp_data = PreprocessGesture(template, gid);

            double dist = DynamicTimeWarping(input_data, temp_data, ACTION_CONFIG[gid].jointWeights);

            return dist;
        }


        /// <summary>
        /// generic dtw algorithm
        /// seq 1 -> seq 2
        /// </summary>
        public double DynamicTimeWarping(
            ArrayList query, ArrayList target, Dictionary<JointType, float> weights)
        {
            if (query == null || target == null || query.Count == 0 || target.Count == 0)
                return -1;

            #region perform DTW to align two arrays
            int N = query.Count;
            int M = target.Count;
            DTW = new double[N + 1, M + 1];   // make an extra space for 0 match

            for (int i = 1; i <= N; i++)
                DTW[i, 0] = double.PositiveInfinity;
            for (int j = 1; j <= M; j++)
                DTW[0, j] = double.PositiveInfinity;
            DTW[0, 0] = 0;

            for (int i = 1; i <= N; i++)
            {
                // match to j position
                for (int j = 1; j <= M; j++)
                {
                    double cost = Tools.Dist2((double[])query[i - 1], (double[])target[j - 1]);
                    DTW[i, j] = cost + Math.Min(DTW[i - 1, j], Math.Min(DTW[i, j - 1], DTW[i - 1, j - 1]));
                }
            }
            #endregion
            

            #region fetch optimal warping paths
            WarpingPath = new List<Point>();
            WarpingPath.Add(new Point(N, M));
            int cur_n = N;
            int cur_m = M;
            while (true)
            {
                if (cur_n == 1 && cur_m == 1)
                {
                    WarpingPath.Add(new Point(1, 1));
                    break;
                }

                if (cur_n == 1 && cur_m != 1)
                {
                    cur_m--;
                    WarpingPath.Add(new Point(cur_n, cur_m));
                    continue;
                }
                if (cur_m == 1 && cur_n != 1)
                {
                    cur_n--;
                    WarpingPath.Add(new Point(cur_n, cur_m));
                    continue;
                }

                // compare dtw value to decide next match
                if (DTW[cur_n - 1, cur_m - 1] < DTW[cur_n - 1, cur_m] &&
                    DTW[cur_n - 1, cur_m - 1] < DTW[cur_n, cur_m - 1])
                {
                    cur_n--;
                    cur_m--;
                    WarpingPath.Add(new Point(cur_n, cur_m));
                    continue;
                }
                if (DTW[cur_n - 1, cur_m] < DTW[cur_n - 1, cur_m - 1] &&
                    DTW[cur_n - 1, cur_m] < DTW[cur_n, cur_m - 1])
                {
                    cur_n--;
                    WarpingPath.Add(new Point(cur_n, cur_m));
                    continue;
                }
                if (DTW[cur_n, cur_m - 1] < DTW[cur_n - 1, cur_m] &&
                    DTW[cur_n, cur_m - 1] < DTW[cur_n - 1, cur_m - 1])
                {
                    cur_m--;
                    WarpingPath.Add(new Point(cur_n, cur_m));
                    continue;
                }
            }
            #endregion
            
            return DTW[N, M];
        }

        public int GetMatchingTargetFrame(int query_frame_id)
        {
            if (WarpingPath == null)
                return -1;

            int target_id = 0;
            foreach (Point curp in WarpingPath)
            {
                if (curp.X == query_frame_id + 1)
                {
                    target_id = (int)curp.Y;
                    break;
                }
            }

            return target_id - 1;   // start from 1
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


        public void GenerateFeedbacks(Action query, Action target)
        {

        }
    }
}
