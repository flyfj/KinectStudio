using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace KinectMotionAnalyzer.Processors
{
    // defined measurements (should be read only)
    public class FMSRule
    {
        public int id;
        public string name = "";
        public List<MeasurementUnit> measurements;

        public FMSRule()
        {
            measurements = new List<MeasurementUnit>();
        }
    }

    //
    public class FMSRuleEvaluation
    {
        public int ruleId;
        public float ruleScore;
        public string feedback = "";

        public FMSRuleEvaluation()
        {
            ruleScore = 0;
        }
    }

    public class FMSTest
    {
        public string testName = "";
        public List<FMSRule> rules = new List<FMSRule>();

        public FMSTest(string name)
        {
            testName = name;
        }
    }

    public class FMSTestEvaluation
    {
        public int testId;
        public float testScore;
        public List<FMSRuleEvaluation> rule_evals = new List<FMSRuleEvaluation>();
    }



    class FMSProcessor
    {
        private MotionAssessor basicAssessor;
        public List<FMSTest> FMSTests = new List<FMSTest>();
        public Dictionary<string, int> FMSTestNameDictionary = new Dictionary<string, int>();

        public FMSProcessor()
        {
            basicAssessor = new MotionAssessor();

            PopulateFMSTests();
        }


        private void PopulateFMSTests()
        {
            // populate test names and ids
            FMSTestNameDictionary.Add("Deep Squat", 0);
            FMSTestNameDictionary.Add("Hurdle Step", 1);

            #region Deep squat
            FMSTest dsquat = new FMSTest("Deep Squat");
            dsquat.rules = new List<FMSRule>();

            FMSRule drule0 = new FMSRule();
            drule0.id = 0;
            drule0.name = "Upper torso is parallel with tibia or toward vertical";
            // measure angle of upper torso relative to ground
            MeasurementUnit drule0_munit1 = new MeasurementUnit(MeasurementType.MType_Angle);
            drule0_munit1.ifSingleJoint = false;
            drule0_munit1.boneJoint1 = JointType.ShoulderCenter;
            drule0_munit1.boneJoint2 = JointType.HipCenter;
            drule0_munit1.plane = PlaneName.XZPlane;
            drule0_munit1.standard_value = 90;
            drule0_munit1.tolerance = 30;
            drule0.measurements.Add(drule0_munit1);
            dsquat.rules.Add(drule0);

            FMSRule drule1 = new FMSRule();
            drule1.id = 1;
            drule1.name = "Femur is below horizontal";
            //drule1.measurements = new List<MeasurementUnit>();
            MeasurementUnit drule1_munit1 = new MeasurementUnit(MeasurementType.MType_PosDiff);
            drule1_munit1.pos_axis = AxisName.YAsix;
            drule1_munit1.joint_higher = JointType.KneeRight;
            drule1_munit1.joint_lower = JointType.HipRight;
            drule1.measurements.Add(drule1_munit1);
            dsquat.rules.Add(drule1);

            //FMSRule drule2 = new FMSRule();
            //drule2.id = 2;
            //drule2.name = "Knees are aligned over feet";
            //drule2.measurements = new List<MeasurementUnit>();
            //MeasurementUnit drule2_munit1 = new MeasurementUnit(MeasurementType.MType_Angle);
            //drule2_munit1.ifSingleJoint = false;
            //drule2_munit1.boneJoint1 = JointType.KneeRight;
            //drule2_munit1.boneJoint2 = JointType.WristRight;
            //drule2_munit1.plane = PlaneName.XZPlane;
            //drule2_munit1.standard_angle_value = 90;
            //drule2_munit1.tolerance = 40;
            //drule2.measurements.Add(drule2_munit1);
            //dsquat.rules.Add(drule2);

            FMSRule drule3 = new FMSRule();
            drule3.id = 3;
            drule3.name = "Dowel should be aligned over feet";
            drule3.measurements = new List<MeasurementUnit>();
            MeasurementUnit drule3_munit1 = new MeasurementUnit(MeasurementType.MType_PosDiff);
            drule3_munit1.pos_axis = AxisName.ZAsix;
            drule3_munit1.joint_higher = JointType.WristRight;
            drule3_munit1.joint_lower = JointType.KneeRight;
            drule3_munit1.standard_value = 0;
            drule3_munit1.tolerance = 0.3;
            drule3.measurements.Add(drule3_munit1);
            dsquat.rules.Add(drule3);

            #endregion

            FMSTests.Add(dsquat);

            #region Hurdle step
            FMSTest hstep = new FMSTest("Hurdle Step");
            FMSRule drule21 = new FMSRule();
            drule21.id = 1;
            drule21.name = "Back straight";
            // measure angle of upper torso relative to ground
            MeasurementUnit drule21_munit1 = new MeasurementUnit(MeasurementType.MType_Angle);
            drule21_munit1.ifSingleJoint = false;
            drule21_munit1.boneJoint1 = JointType.ShoulderCenter;
            drule21_munit1.boneJoint2 = JointType.HipCenter;
            drule21_munit1.plane = PlaneName.XZPlane;
            drule21_munit1.standard_value = 90;
            drule21_munit1.tolerance = 20;
            drule21.measurements.Add(drule21_munit1);
            hstep.rules.Add(drule21);

            FMSRule drule22 = new FMSRule();
            drule22.id = 2;
            drule22.name = "No move for lumbar spine";
            //drule1.measurements = new List<MeasurementUnit>();
            MeasurementUnit drule22_munit1 = new MeasurementUnit(MeasurementType.MType_PosDiff);
            drule22_munit1.pos_axis = AxisName.XAxis;
            drule22_munit1.joint_higher = JointType.KneeRight;
            drule22_munit1.joint_lower = JointType.HipRight;
            drule22_munit1.standard_value = 0;
            drule22_munit1.tolerance = 0.1;
            drule22.measurements.Add(drule22_munit1);
            hstep.rules.Add(drule22);

            FMSRule drule23 = new FMSRule();
            drule23.id = 3;
            drule23.name = "Dowel horizontal";
            //drule1.measurements = new List<MeasurementUnit>();
            MeasurementUnit drule23_munit1 = new MeasurementUnit(MeasurementType.MType_PosDiff);
            drule23_munit1.pos_axis = AxisName.YAsix;
            drule23_munit1.joint_higher = JointType.WristLeft;
            drule23_munit1.joint_lower = JointType.WristRight;
            drule23_munit1.standard_value = 0;
            drule23_munit1.tolerance = 0.1;
            drule23.measurements.Add(drule23_munit1);
            hstep.rules.Add(drule23);
            #endregion

            FMSTests.Add(hstep);
        }

        public FMSRuleEvaluation EvaluateRule(List<Skeleton> skeletons, int testId, FMSRule rule)
        {
            FMSRuleEvaluation rule_eval = new FMSRuleEvaluation();
            rule_eval.ruleId = rule.id;
            if (skeletons != null && skeletons.Count > 0)
            {
                // deep squat
                if (testId == 0)
                {
                    if (rule.id == 1)
                    {
                        // find the smallest difference between two joint positions
                        List<float> pos_diff = new List<float>();
                        foreach (Skeleton ske in skeletons)
                        {
                            if(ske == null)
                                continue;

                            float diff = ske.Joints[rule.measurements[0].joint_higher].Position.Y - 
                                ske.Joints[rule.measurements[0].joint_lower].Position.Y;
                            pos_diff.Add(diff);
                        }

                        MessageBox.Show("rule1: " + pos_diff.Max());
                        
                        if (pos_diff.Max() >= 0)
                            rule_eval.ruleScore = 1;
                        else
                            rule_eval.ruleScore = 0;
                    }
                    if (rule.id == 2 || rule.id == 3 || rule.id == 0)
                    {
                        // if angle is 90 relative to ground when reaching the lowest point
                        float min_pos_y = float.MaxValue;
                        int sel_id = -1;
                        for (int i = 0; i < skeletons.Count; i++)
                        {
                            if (skeletons[i] == null)
                                continue;

                            float diff = skeletons[i].Joints[JointType.Spine].Position.Y;

                            if (diff < min_pos_y)
                            {
                                min_pos_y = diff;
                                sel_id = i;
                            }
                        }

                        double angle_val = basicAssessor.ComputeMeasurement(skeletons[sel_id], rule.measurements[0]);

                        MessageBox.Show(rule.id + ": " + angle_val);

                        //JointStatus status = new JointStatus();
                        //basicAssessor.ComputeJointAngle(skeletons[sel_id], rule.measurements[0], ref status);
                        //double angle = status.planeAngles[rule.measurements[0].boneJoint2][rule.measurements[0].plane];
                        if (Math.Abs(angle_val - rule.measurements[0].standard_value) < rule.measurements[0].tolerance)
                            rule_eval.ruleScore = 1;
                        else
                            rule_eval.ruleScore = 0;
                    }
                }
                if (testId == 1)
                {
                    // find the time right knee is the higher
                    double max_knee_y = -1;
                    int sel_id = 0;
                    for (int i = 0; i < skeletons.Count; i++)
                    {
                        if (skeletons[i] == null)
                            continue;

                        float diff = skeletons[i].Joints[JointType.KneeRight].Position.Y;

                        if (diff > max_knee_y)
                        {
                            max_knee_y = diff;
                            sel_id = i;
                        }
                    }

                    double mval = basicAssessor.ComputeMeasurement(skeletons[sel_id], rule.measurements[0]);

                    //MessageBox.Show(rule.id + ": " + mval);

                    //JointStatus status = new JointStatus();
                    //basicAssessor.ComputeJointAngle(skeletons[sel_id], rule.measurements[0], ref status);
                    //double angle = status.planeAngles[rule.measurements[0].boneJoint2][rule.measurements[0].plane];
                    if (Math.Abs(mval - rule.measurements[0].standard_value) < rule.measurements[0].tolerance)
                        rule_eval.ruleScore = 1;
                    else
                        rule_eval.ruleScore = 0;
                }
            }

            return rule_eval;
        }

        public FMSTestEvaluation EvaluateTest(List<Skeleton> skeletons, string testName)
        {
            if (!FMSTestNameDictionary.ContainsKey(testName))
                return null;

            int testId = FMSTestNameDictionary[testName];
            FMSTestEvaluation test_eval = new FMSTestEvaluation();

            int rule_ok_num = 0;
            foreach (FMSRule rule in FMSTests[testId].rules)
            {
                FMSRuleEvaluation rule_eval = EvaluateRule(skeletons, testId, rule);
                rule_ok_num += (rule_eval.ruleScore == 1 ? 1 : 0);
                test_eval.rule_evals.Add(rule_eval);
            }

            switch (rule_ok_num)
            {
                case 3:
                    test_eval.testScore = 3;
                    break;
                case 2:
                    test_eval.testScore = 2;
                    break;
                case 1:
                    test_eval.testScore = 1;
                    break;
                default:
                    test_eval.testScore = 0;
                    break;
            }

            return test_eval;
        }

        public int FMSName2Id(string testName)
        {
            if (FMSTestNameDictionary.ContainsKey(testName))
                return FMSTestNameDictionary[testName];
            else
                return -1;
        }
    }
}
