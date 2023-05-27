using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RBFSlover))]

public class RBFSloverUI : Editor
{

    private int selectpose;
    private string[] posenamelist;   
       
    
    public override void OnInspectorGUI()
    {

        base.OnInspectorGUI();
        RBFSlover script = target as RBFSlover;

        EditorGUILayout.LabelField("--------------------被驱动物体-----------------");
        script.OutputReference = (Transform)EditorGUILayout.ObjectField("被驱动物体参考节点", script.OutputReference, typeof(Transform), true);

        EditorGUI.BeginChangeCheck();
        script.driverNum = EditorGUILayout.IntField("被驱动物体数量",script.driverNum);
        if (EditorGUI.EndChangeCheck())
        {
            //script.shuchu.Capacity = script.shuchulength;
            //script.shuchu = new List<RBFSlover.Shuchu>(script.shuchulength);
            while (script.driverList.Count != script.driverNum)
            {
                if (script.driverList.Count > script.driverNum)
                {
                    script.driverList.RemoveAt(script.driverList.Count-1);
                    continue;
                }

                if (script.driverList.Count < script.driverNum)
                {
                    script.driverList.Add(new RBFSlover.Driver());
                    continue;
                }

            }
            
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("被驱动物体");
        EditorGUILayout.LabelField("被驱动类型");
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i< script.driverNum; i++)
        {
            EditorGUILayout.BeginHorizontal();

            script.driverList[i].OutputObject = (Transform)EditorGUILayout.ObjectField(script.driverList[i].OutputObject, typeof(Transform), true);
            script.driverList[i].OutputType =(RBFSlover.ControlTYpe)EditorGUILayout.EnumPopup(script.driverList[i].OutputType);

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.EndHorizontal();


        EditorGUILayout.LabelField("-------------------Pose参数------------------");
        script.posename = EditorGUILayout.TextField("Pose名称", script.posename);

        if (script.PoseList.Count > 0)
        {
            posenamelist = new string[script.PoseList.Count];
            for (int i = 0; i < script.PoseList.Count; i++)
            {
                posenamelist[i] = script.PoseList[i].name;
            }

            selectpose = EditorGUILayout.Popup(selectpose, posenamelist);

            for (int i = 0; i < script.PoseList[selectpose].input.Count; i++)
            {
                EditorGUILayout.LabelField("控制器" + i + script.PoseList[selectpose].input[i]);
            }

            
            for (int i = 0; i < script.PoseList[selectpose].output.Count; i++)
            {
                EditorGUILayout.LabelField("被驱动物体" + i + script.PoseList[selectpose].output[i]);
            }
        }

        //EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Pose") && (script.MainController != null) && (script.driverList != null))
        {
            
            script.PoseList.Add(CreateCurrentPose(script));

        }

        if (GUILayout.Button("Delete Pose") && (script.driverList != null))
        {
            script.PoseList.RemoveAt(selectpose);
        }

        if (GUILayout.Button("Update Pose") && (script.driverList != null))
        {
            string tempname = script.PoseList[selectpose].name;
            script.PoseList.RemoveAt(selectpose);
            script.PoseList.Insert(selectpose, CreateCurrentPose(script));
            script.PoseList[selectpose].name = tempname;
        }

        if (GUILayout.Button("Clear Pose") && (script.driverList != null))
        {
            script.PoseList.Clear();
        }

        if (GUILayout.Button("View Pose") && (script.driverList != null))
        {
            
            SetSelectPose(script, selectpose);
        }

        /*if (GUILayout.Button("Calculate") && (script.driverList != null))
        {
            script.CalculateSimMatrix();
        }*/

        string state;
        if (script.running)
        {
            state = "当前计算模式，点击进入编辑模式";
            script.CalculateSimMatrix();
            //script.CalculateCurrentOut();
        }
        else
        {
            state = "当前编辑状态，点击进入计算模式";
        }

        if (GUILayout.Button(state))
        {
            script.running = !script.running;
        }




        Repaint();
    }

    //建立当前Pose
    private RBFSlover.Pose CreateCurrentPose(RBFSlover slover)
    {
        RBFSlover.Pose temppose = new RBFSlover.Pose();

        temppose.name = slover.PoseList.Count + slover.posename;

        

        foreach (GameObject item in slover.MainController)
        {
            
            switch (slover.MainControlType)
            {
                case RBFSlover.ControlTYpe.Position:
                                        
                    temppose.input.Add(slover.RootBone.InverseTransformPoint(item.transform.position));                    
                    
                    break;

                case RBFSlover.ControlTYpe.OrientationX:
                                        
                    temppose.input.Add(slover.RootBone.InverseTransformDirection(item.transform.right));
                    
                    break;

                case RBFSlover.ControlTYpe.OrientationY:

                    temppose.input.Add(slover.RootBone.InverseTransformDirection(item.transform.up));

                    break;

                case RBFSlover.ControlTYpe.OrientationZ:

                    temppose.input.Add(slover.RootBone.InverseTransformDirection(item.transform.forward));

                    break;

                case RBFSlover.ControlTYpe.Rotation:
                                        
                    Quaternion q = Quaternion.Inverse(slover.RootBone.rotation) * item.transform.rotation;

                    temppose.input.Add(new Vector4(q.x, q.y, q.z, q.w));
                    
                    break;

            }

            temppose.rotation.Add(Quaternion.Inverse(slover.RootBone.rotation) * item.transform.rotation);//记录旋转用以复原
        }

        foreach (RBFSlover.Driver item in slover.driverList)
        {
            switch (item.OutputType)
            {
                case RBFSlover.ControlTYpe.Position:

                    
                    if (item.OutputObject != null)
                    {
                        Vector3 temp = slover.OutputReference.InverseTransformPoint(item.OutputObject.position);

                        temppose.output.Add(new Vector4(temp.x, temp.y, temp.z, 0.0f));

                    }
                    

                    break;

                case RBFSlover.ControlTYpe.OrientationX:                    

                    if (item.OutputObject != null)
                    {
                        Vector3 temp = slover.OutputReference.InverseTransformDirection(item.OutputObject.right);

                        temppose.output.Add(new Vector4(temp.x, temp.y, temp.z, 0.0f));

                    }                    

                    break;

                case RBFSlover.ControlTYpe.OrientationY:

                    if (item.OutputObject != null)
                    {
                        Vector3 temp = slover.OutputReference.InverseTransformDirection(item.OutputObject.up);

                        temppose.output.Add(new Vector4(temp.x, temp.y, temp.z, 0.0f));

                    }

                    break;

                case RBFSlover.ControlTYpe.OrientationZ:

                    if (item.OutputObject != null)
                    {
                        Vector3 temp = slover.OutputReference.InverseTransformDirection(item.OutputObject.forward);

                        temppose.output.Add(new Vector4(temp.x, temp.y, temp.z, 0.0f));

                    }

                    break;

                case RBFSlover.ControlTYpe.Rotation:                    

                    if (item.OutputObject != null)
                    {
                        Quaternion tempq = Quaternion.Inverse(slover.OutputReference.rotation) * item.OutputObject.rotation;

                        temppose.output.Add(new Vector4(tempq.x, tempq.y, tempq.z, tempq.w));

                    }                    

                    break;
            }
        }

        return temppose;
    }

    //设置成当前选中的Pose
    private void SetSelectPose(RBFSlover slover, int index)
    {
        
        for (int i = 0; i < slover.MainController.Count; i++)
        {
            switch (slover.MainControlType)
            {
                case RBFSlover.ControlTYpe.Position:
                    
                    slover.MainController[i].transform.position = slover.RootBone.TransformPoint((Vector3)slover.PoseList[index].input[i]);                    

                    break;

                case RBFSlover.ControlTYpe.OrientationX:
                    
                    slover.MainController[i].transform.rotation = slover.RootBone.rotation * slover.PoseList[index].rotation[i];                    

                    break;

                case RBFSlover.ControlTYpe.OrientationY:

                    slover.MainController[i].transform.rotation = slover.RootBone.rotation * slover.PoseList[index].rotation[i];

                    break;

                case RBFSlover.ControlTYpe.OrientationZ:

                    slover.MainController[i].transform.rotation = slover.RootBone.rotation * slover.PoseList[index].rotation[i];

                    break;

                case RBFSlover.ControlTYpe.Rotation:
                    
                    slover.MainController[i].transform.rotation = slover.RootBone.rotation * slover.PoseList[index].rotation[i];                    

                    break;


            }
        }

                

        for (int i = 0; i < slover.driverList.Count; i++)
        {
            switch (slover.driverList[i].OutputType)
            {
                case RBFSlover.ControlTYpe.Position:


                    if (slover.driverList[i].OutputObject != null)
                    {
                        slover.driverList[i].OutputObject.position = slover.OutputReference.TransformPoint((Vector3)slover.PoseList[index].output[i]);

                    }


                    break;

                case RBFSlover.ControlTYpe.OrientationX:

                    //
                    //
                    //

                    break;

                case RBFSlover.ControlTYpe.OrientationY:
                              
                    
                    break;

                case RBFSlover.ControlTYpe.OrientationZ:


                    break;

                case RBFSlover.ControlTYpe.Rotation:

                    if (slover.driverList[i].OutputObject != null)
                    {
                        Vector4 q = slover.PoseList[index].output[i];
                        Quaternion tempq = new Quaternion(q.x, q.y, q.z, q.w);
                        slover.driverList[i].OutputObject.rotation = slover.OutputReference.rotation * tempq;

                    }

                    break;
            }
        }


    }
}
