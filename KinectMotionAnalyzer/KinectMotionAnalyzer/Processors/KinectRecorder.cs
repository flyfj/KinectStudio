using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.Kinect;


namespace KinectMotionAnalyzer.Processors
{
    /// <summary>
    /// general recorder to capture and save kinect data
    /// usually, store temporal data in memory and save to file one time only
    /// </summary>
    class KinectRecorder
    {

        /// <summary>
        /// save skeleton data to xml file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="data">skeleton from each time stamp</param>
        /// <returns></returns>
        static public bool WriteToSkeletonFile(string filename, List<Skeleton> data)
        {

            XmlDocument xmldoc = new XmlDocument();
            XmlDeclaration declar = xmldoc.CreateXmlDeclaration("1.0", null, null);
            xmldoc.AppendChild(declar);
            // create root element
            XmlElement root = xmldoc.CreateElement("Skeletons");
            xmldoc.AppendChild(root);

            #region save_frames
            for (int i = 0; i < data.Count; i++ )
            {
                XmlElement frame_elem = xmldoc.CreateElement("Frame");
                // good habit to add right after creation to prevent forgetting later
                root.AppendChild(frame_elem);

                frame_elem.SetAttribute("Id", i.ToString());

                #region output_skeletons

                Skeleton ske = data[i];

                // create each skeleton
                XmlElement skeleton_elem = xmldoc.CreateElement("Skeleton");
                frame_elem.AppendChild(skeleton_elem);

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

        static public List<Skeleton> ReadFromSkeletonFile(string filename)
        {

            XmlDocument doc = new XmlDocument();
            doc.Load(filename);

            List<Skeleton> skeletonsCollection = new List<Skeleton>();
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

            return skeletonsCollection;
        }
    }
}
