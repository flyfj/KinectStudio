using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using Microsoft.Kinect;
using KinectMotionAnalyzer.Model;

namespace KinectMotionAnalyzer.Processors
{
    /// <summary>
    /// general recorder to capture and save kinect data
    /// usually, store temporal data in memory and save to file one time only
    /// </summary>
    class KinectRecorder
    {

        /// <summary>
        /// save skeleton data to xml file; assume one skeleton for each frame
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="data">skeleton from each time stamp</param>
        /// <returns></returns>
        static public bool WriteToSkeletonXMLFile(string filename, List<Skeleton> data)
        {

            XmlDocument xmldoc = new XmlDocument();
            XmlDeclaration declar = xmldoc.CreateXmlDeclaration("1.0", null, null);
            xmldoc.AppendChild(declar);
            // create root element <Skeletons>
            XmlElement root = xmldoc.CreateElement("Skeletons");
            xmldoc.AppendChild(root);

            #region save_frames
            for (int i = 0; i < data.Count; i++ )
            {
                XmlElement frame_elem = xmldoc.CreateElement("Frame");
                // good habit to add right after creation to prevent forgetting later
                root.AppendChild(frame_elem);   // <Frame Id=...>

                frame_elem.SetAttribute("Id", i.ToString());

                #region output_skeletons

                Skeleton ske = data[i];

                // create each skeleton
                XmlElement skeleton_elem = xmldoc.CreateElement("Skeleton");
                frame_elem.AppendChild(skeleton_elem);  // <Skeleton>

                skeleton_elem.SetAttribute("Id", ske.TrackingId.ToString());
                skeleton_elem.SetAttribute("State", ske.TrackingState.ToString());
                if (ske.TrackingState != SkeletonTrackingState.NotTracked)
                {
                    // write position first
                    XmlElement pos_elem = xmldoc.CreateElement("Position");
                    pos_elem.SetAttribute("posx", ske.Position.X.ToString());
                    pos_elem.SetAttribute("posy", ske.Position.Y.ToString());
                    pos_elem.SetAttribute("posz", ske.Position.Z.ToString());
                    skeleton_elem.AppendChild(pos_elem);

                    #region Output_joints

                    XmlElement joints_elem = xmldoc.CreateElement("Joints");
                    skeleton_elem.AppendChild(joints_elem);

                    // add joints
                    foreach (Joint joint in ske.Joints)
                    {
                        XmlElement joint_elem = xmldoc.CreateElement("Joint");
                        joint_elem.SetAttribute("Type", joint.JointType.ToString());
                        int jtype = (int)joint.JointType;
                        joint_elem.SetAttribute("TypeId", jtype.ToString());
                        joint_elem.SetAttribute("State", joint.TrackingState.ToString());
                        //XmlElement joint_rotation_elem = xmldoc.CreateElement("Rotation");
                        XmlElement joint_pos_elem = xmldoc.CreateElement("Position");
                        joint_pos_elem.SetAttribute("posx", joint.Position.X.ToString());
                        joint_pos_elem.SetAttribute("posy", joint.Position.Y.ToString());
                        joint_pos_elem.SetAttribute("posz", joint.Position.Z.ToString());
                        //joint_elem.SetAttribute("Orientation", ske.BoneOrientations[joint.JointType].AbsoluteRotation.

                        joint_elem.AppendChild(joint_pos_elem);
                        joints_elem.AppendChild(joint_elem);
                    }
                    #endregion
                }
                #endregion
            }
            #endregion

            xmldoc.Save(filename);

            return true;
        }

        static public bool ReadFromSkeletonXMLFile(string filename, out List<Skeleton> skeletonsCollection)
        {

            XmlDocument doc = new XmlDocument();
            doc.Load(filename);

            skeletonsCollection = new List<Skeleton>();
            XmlElement root = doc.DocumentElement;
            

            #region read_frames
            for (int i = 0; i < root.ChildNodes.Count; i++ )
            {
                // read each frame element
                XmlElement frame_elem = root.ChildNodes[i] as XmlElement;
                int frame_id = int.Parse(frame_elem.Attributes["Id"].Value);

                if(frame_elem.ChildNodes.Count != 1)
                    continue;

                XmlElement ske_elem = frame_elem.ChildNodes[0] as XmlElement;
                int id = int.Parse(ske_elem.Attributes["Id"].Value);
                string state = ske_elem.Attributes["State"].Value;

                Skeleton cur_skeleton = new Skeleton();
                cur_skeleton.TrackingId = id;
                cur_skeleton.TrackingState = SkeletonTrackingState.NotTracked;

                #region input_joints
                if (state != SkeletonTrackingState.NotTracked.ToString())
                {
                    cur_skeleton.TrackingState = SkeletonTrackingState.PositionOnly;

                    // get position
                    XmlElement pos_elem = ske_elem.ChildNodes[0] as XmlElement;
                    SkeletonPoint position = new SkeletonPoint();
                    position.X = float.Parse(pos_elem.Attributes["posx"].Value);
                    position.Y = float.Parse(pos_elem.Attributes["posy"].Value);
                    position.Y = float.Parse(pos_elem.Attributes["posz"].Value);

                    cur_skeleton.Position = position;   // set value

                    #region read_joint
                    if (state == SkeletonTrackingState.Tracked.ToString())
                    {
                        cur_skeleton.TrackingState = SkeletonTrackingState.Tracked;

                        // read joints
                        foreach (XmlElement joint_elem in ske_elem.ChildNodes[1])
                        {
                            int jointtype = int.Parse(joint_elem.Attributes["TypeId"].Value);
                            JointType type = (JointType)jointtype;

                            // get a copy of current joint 
                            // (jointtype is read-only, this is the way we get it)
                            Joint cur_joint = cur_skeleton.Joints[type];

                            XmlElement joint_pos_elem = joint_elem.ChildNodes[0] as XmlElement;

                            SkeletonPoint joint_pos = new SkeletonPoint();
                            joint_pos.X = float.Parse(joint_pos_elem.Attributes["posx"].Value);
                            joint_pos.Y = float.Parse(joint_pos_elem.Attributes["posy"].Value);
                            joint_pos.Z = float.Parse(joint_pos_elem.Attributes["posz"].Value);

                            cur_joint.Position = joint_pos;
                            cur_joint.TrackingState = JointTrackingState.Tracked;
                                
                            // copy back to update
                            cur_skeleton.Joints[type] = cur_joint;
                                
                        }
                    }
                    #endregion
                }
                #endregion

                // add to dictionary
                skeletonsCollection.Add(cur_skeleton);
                
            }
            #endregion

            return true;
        }

        static public bool WriteToSkeletonFile(string filename, List<Skeleton> data)
        {
            if (data == null)
                return false;

            using (var file = File.OpenWrite(filename + ".ske"))
            {
                // output skeleton joints
                BinaryWriter writer = new BinaryWriter(file);
                writer.Write(data.Count);
                foreach (Skeleton ske in data)
                {
                    foreach (JointType type in ske.Joints)
                    {
                        writer.Write(ske.Joints[type].Position.X);
                        writer.Write(ske.Joints[type].Position.Y);
                        writer.Write(ske.Joints[type].Position.Z);
                    }
                }
            }

            return true;
        }

        static public bool ReadFromSkeletonFile(string filename, out List<Skeleton> skeletonsCollection)
        {
            byte[] bytes = File.ReadAllBytes(filename + ".ske");
            int frameNum = BitConverter.ToInt32(bytes, 0);
            skeletonsCollection = new List<Skeleton>(frameNum);

            int count = 4;
            foreach (Skeleton ske in skeletonsCollection)
            {
                foreach (JointType type in ske.Joints)
                {
                    Joint joint = new Joint();
                    SkeletonPoint pt = new SkeletonPoint();
                    pt.X = BitConverter.ToSingle(bytes, count);
                    count += 4;
                    pt.Y = BitConverter.ToSingle(bytes, count);
                    count += 4;
                    pt.Z = BitConverter.ToSingle(bytes, count);
                    count += 4;
                    joint.Position = pt;
                    ske.Joints[type] = joint;
                }
            }

            return true;
        }

        static public bool WriteToColorImageFile(string filename, List<byte[]> colorData)
        {
            if (colorData == null)
                return false;

            using (var file = File.OpenWrite(filename + ".img"))
            {
                // width and height and frame number
                BinaryWriter writer = new BinaryWriter(file);
                writer.Write(640);
                writer.Write(480);
                writer.Write(colorData.Count);
                foreach (byte[] colorFrame in colorData)
                {
                    foreach (byte val in colorFrame)
                        writer.Write(val);
                }
            }

            return true;
        }

        static public bool ReadFromColorImageFile(string filename, out List<byte[]> colorData)
        {
            var bytes = File.ReadAllBytes(filename + ".img");
            int width = BitConverter.ToInt32(bytes, 0);
            int height = BitConverter.ToInt32(bytes, 4);
            int frameNum = BitConverter.ToInt32(bytes, 8);
            colorData = new List<byte[]>();
            for (int i = 0; i < frameNum; i++)
            {
                // read color data from file
                colorData.Add(Enumerable.Range(width * height * i + 12, width * height).Select(x => bytes[x]).ToArray());
            }

            return true;
        }

        static public bool SaveAllToFile(string filename, List<byte[]> colorData, List<Skeleton> skeData)
        {
            // save data for each frame to individual files (color and skeleton) for now (may merge to one file later)
            if (!WriteToColorImageFile(filename, colorData))
                return false;

            if (!WriteToSkeletonFile(filename, skeData))
                return false;

            return true;
        }

        static public bool ReadAllFromFile(string filename, out List<byte[]> colorData, out List<Skeleton> skeData)
        {
            colorData = null;
            skeData = null;

            if (!ReadFromColorImageFile(filename, out colorData))
                return false;

            if (!ReadFromSkeletonFile(filename, out skeData))
                return false;

            return true;
        }

        static public bool WriteToConfigFile(string filename)
        {
            return true;
        }

        //////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// database access  
        /// </summary>
        static public bool WriteActionToDatabase(KinectAction action)
        {
            // get database context
            using (MotionDBContext motionContext = new MotionDBContext())
            {
                try
                {
                    //if (motionContext.Database.Exists())
                    //    motionContext.Database.Delete();

                    motionContext.Actions.Add(action);
                    motionContext.SaveChanges();
                }
                catch (System.Exception ex)
                {
                	MessageBox.Show(ex.Message);
                    return false;
                }

                //foreach (var q in query)
                //{
                //    Console.WriteLine((q as KinectAction).Id);
                //}
            }

            return true;
        }
    }
}
