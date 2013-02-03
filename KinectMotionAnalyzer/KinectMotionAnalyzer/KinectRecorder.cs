using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.Kinect;


namespace KinectMotionAnalyzer
{
    /// <summary>
    /// general recorder to capture and save kinect data
    /// usually, store temporal data in memory and save to file one time only
    /// </summary>
    class KinectRecorder
    {
        // skeleton
        static public bool WriteToSkeletonFile(string filename, Skeleton[] data)
        {

            XmlDocument xmldoc = new XmlDocument();
            XmlDeclaration declar = xmldoc.CreateXmlDeclaration("1.0", null, null);
            xmldoc.AppendChild(declar);
            // create root element
            XmlElement root = xmldoc.CreateElement("Skeletons");
            xmldoc.AppendChild(root);

            foreach(Skeleton ske in data)
            {
                // create each skeleton
                XmlElement skeleton_elem = xmldoc.CreateElement("Skeleton");
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

                    if (ske.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        XmlElement joints_elem = xmldoc.CreateElement("Joints");
                        skeleton_elem.AppendChild(joints_elem);
                        // add joints
                        foreach (Joint joint in ske.Joints)
                        {
                            XmlElement joint_elem = xmldoc.CreateElement("Joint");
                            joint_elem.SetAttribute("Type", joint.JointType.ToString());
                            joint_elem.SetAttribute("TypeId", int.Parse(joint.JointType.ToString()).ToString());
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
                    }
                }

                root.AppendChild(skeleton_elem);
            }

            xmldoc.Save(filename);

            return true;
        }

        static public Skeleton[] ReadFromSkeletonFile(string filename)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filename);

            XmlElement root = doc.DocumentElement;
            Skeleton[] skeletons = new Skeleton[root.ChildNodes.Count];

            for (int i = 0; i < root.ChildNodes.Count; i++ )
            {
                Skeleton cur_skeleton = new Skeleton();

                XmlElement ske_elem = root.ChildNodes[i] as XmlElement;
                int id = int.Parse(ske_elem.Attributes["Id"].Value);
                string state = ske_elem.Attributes["State"].Value;

                cur_skeleton.TrackingId = id;
                cur_skeleton.TrackingState = SkeletonTrackingState.NotTracked;

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

                    if (state == SkeletonTrackingState.Tracked.ToString())
                    {
                        cur_skeleton.TrackingState = SkeletonTrackingState.Tracked;

                        // read joints
                        foreach (XmlElement joint_elem in ske_elem.ChildNodes[1])
                        {
                            int jointtype = int.Parse(joint_elem.Attributes["TypeId"].Value);
                            JointType type = (JointType)jointtype;
                            XmlElement joint_pos_elem = joint_elem.ChildNodes[0] as XmlElement;

                            SkeletonPoint joint_pos = new SkeletonPoint();
                            joint_pos.X = float.Parse(joint_pos_elem.Attributes["posx"].Value);
                            joint_pos.Y = float.Parse(joint_pos_elem.Attributes["posy"].Value);
                            joint_pos.Z = float.Parse(joint_pos_elem.Attributes["posz"].Value);

                            Joint joint = new Joint();
                            joint.Position = joint_pos;
                            joint.TrackingState = JointTrackingState.Tracked;
                            cur_skeleton.Joints[type] = joint;
                        }
                    }
                }

                skeletons[i] = cur_skeleton;

            }

            return skeletons;
        }
    }
}
