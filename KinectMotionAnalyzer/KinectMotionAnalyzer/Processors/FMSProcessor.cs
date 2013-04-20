using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace KinectMotionAnalyzer.Processors
{
    // defined measurements (should be read only)
    public class FMSRule
    {
        public int id;
        public string name;
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
        public string feedback;

        public FMSRuleEvaluation()
        {
            ruleScore = 0;
        }
    }

    public class FMSTest
    {
        public string testName;
        public List<FMSRule> rules;

        public FMSTest(string name)
        {
            testName = name;
        }
    }

    public class FMSTestEvaluation
    {
        public int testId;
        public float testScore;
    }



    class FMSProcessor
    {
        private MotionAssessor basicAssessor;
        private List<FMSTest> FMSTests;

        public FMSProcessor()
        {
            basicAssessor = new MotionAssessor();

            // populate test
            FMSTests = new List<FMSTest>();
            // deep squat
            FMSTest dsquat = new FMSTest("Deep Squat");
            dsquat.rules = new List<FMSRule>();
            FMSRule drule1 = new FMSRule();
            drule1.id = 1;
            drule1.name = "Femur is below horizontal";
            drule1.measurements = new List<MeasurementUnit>();
            MeasurementUnit drule1_munit1 = new MeasurementUnit(MeasurementType.MType_PosDiff);
            drule1_munit1.joint_higher = JointType.KneeRight;
            drule1_munit1.joint_lower = JointType.HipRight;
            drule1.measurements.Add(drule1_munit1);
            dsquat.rules.Add(drule1);

            FMSRule drule2 = new FMSRule();
            drule2.id = 2;
            drule2.measurements = new List<MeasurementUnit>();
            MeasurementUnit drule2_munit1 = new MeasurementUnit(MeasurementType.MType_Angle);
            drule2_munit1.ifSingleJoint = false;
            drule2_munit1.boneJoint1 = JointType.KneeRight;
            drule2_munit1.boneJoint2 = JointType.FootRight;
            drule2_munit1.plane = PlaneName.XZPlane;
            drule2.measurements.Add(drule2_munit1);
            dsquat.rules.Add(drule2);

            FMSTests.Add(dsquat);
            // hurdle step
            FMSTest hstep = new FMSTest("Hurdle Step");

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
                            float diff = ske.Joints[rule.measurements[0].joint_higher].Position.Y - 
                                ske.Joints[rule.measurements[0].joint_lower].Position.Y;
                            pos_diff.Add(diff);
                        }
                        if (pos_diff.Max() >= 0)
                            rule_eval.ruleScore = 1;
                        else
                            rule_eval.ruleScore = 0;
                    }
                    if (rule.id == 2 || rule.id == 3)
                    {
                        // if angle is 90 relative to ground when reaching the lowest point
                        float max_diff = float.MinValue;
                        int sel_id = -1;
                        for (int i = 0; i < skeletons.Count; i++)
                        {
                            float diff = skeletons[i].Joints[JointType.KneeRight].Position.Y -
                                skeletons[i].Joints[JointType.FootRight].Position.Y;

                            if (diff > max_diff)
                            {
                                max_diff = diff;
                                sel_id = i;
                            }
                        }

                        JointStatus status = new JointStatus();
                        basicAssessor.ComputeJointAngle(skeletons[sel_id], rule.measurements[0], ref status);
                        double angle = status.planeAngles[rule.measurements[0].boneJoint2][rule.measurements[0].plane];
                        if (angle < 10)
                            rule_eval.ruleScore = 1;
                        else
                            rule_eval.ruleScore = 0;
                    }
                }
            }

            return rule_eval;
        }

        public FMSTestEvaluation EvaluateTest(List<Skeleton> skeletons, FMSTest test)
        {
            FMSTestEvaluation test_eval = new FMSTestEvaluation();

            return test_eval;
        }
    }
}
