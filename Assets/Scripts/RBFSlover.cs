using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///https://numerics.mathdotnet.com/api/MathNet.Numerics.Integration/GaussLegendreRule.htm
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

[ExecuteInEditMode]
public class RBFSlover : MonoBehaviour
{
    [Header("控制器参考节点")]
    public Transform RootBone;

    [Header("主控制器")]
    public List<GameObject> MainController;
    
    public enum ControlTYpe
    {
        Position,
        OrientationX,
        OrientationY,
        OrientationZ,
        Rotation,
        //Weight,
        //RGBA
    }

    [Header("控制器驱动类型")]
    public ControlTYpe MainControlType;

    //[Header("被驱动器参考节点")]
    [HideInInspector]
    public Transform OutputReference;

    //[HideInInspector]
    //[Header("被驱动物体")]
    //public List<Transform> Output;

    //[HideInInspector]
    //[Header("被驱动物体类型")]
    //public ControlTYpe OutputControlType;

    [System.Serializable]
    public class Driver
    {
        public Transform OutputObject;
        public ControlTYpe OutputType;
    }


    //[HideInInspector]
    //public int controllerNum = 0;//控制器物体数量
    //[HideInInspector]
    //public List<Driver> controllerList = new List<Driver>();//控制器物体列表

    [HideInInspector]
    public int driverNum = 0;//被驱动物体数量
    [HideInInspector]
    public List<Driver> driverList = new List<Driver>();//被驱动物体列表
        

    [HideInInspector]
    public string posename;

    [HideInInspector]
    public bool running= false;

    private int resultitemlength = 4;

    //[Header("参考")]
    //public List<Transform> Pose;
    
    [System.Serializable]
    public class Pose 
    {
        public string name;
        public List<Vector4> input = new List<Vector4>();
        public List<Quaternion> rotation = new List<Quaternion>();
        public List<Vector4> output = new List<Vector4>();
    }

    [HideInInspector]
    //存储Pose
    public List<Pose> PoseList = new List<Pose>();

    
    //Weight矩阵
    private DenseMatrix M_Weight;

    private void OnValidate()
    {
        /*foreach (var item in PoseList)
        {
            item.Output.Clear();
            item.Output.AddRange(Output);
        }*/
        CalculateSimMatrix();

    }

    private void OnEnable()
    {
        CalculateSimMatrix();
    }

    // Start is called before the first frame update
    void Start()
    {
        //Debug.Log(M_Weight);
        running = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void LateUpdate()
    {
        if (running)
            CalculateCurrentOut();
    }

    //高斯核
    private float Gaussian(float x,float r)
    {
        float result = 0;

        r = Mathf.Max(r, 0.001f);
        result = Mathf.Exp(-x * x / (r * r));

        return result;
    }

    //计算范数//线性
    private float CalculateFanshu(List<Vector4> a , List<Vector4> b)
    {
        float result = 0;
        float sum = 0;

        for (int i = 0; i < a.Count;i++)
        {
            sum  = sum + Vector4.Distance(a[i],b[i]);
        }

        result = Mathf.Sqrt(sum);

        return result;
    }

    //计算相似度矩阵
    public void CalculateSimMatrix()
    {
        if (PoseList.Count > 0)
        {
            int posecount = PoseList.Count;//Pose Number
            var M_Similart = new DenseMatrix(posecount);//相似度矩阵

            for (int i = 0; i < posecount; i++)
            {
                for (int j = 0; j < posecount; j++)
                {
                    //float fanshu = Vector4.Distance(PoseList[i].input, PoseList[j].input);//范数使用欧式范数
                    float fanshu = CalculateFanshu(PoseList[i].input, PoseList[j].input);//范数使用欧式范数

                    //核函数 高斯
                    //float sim = Gaussian(fanshu , 10.0f);
                    float sim = fanshu;

                    M_Similart[i, j] = sim;
                    M_Similart[j, i] = sim;
                }

            }
                        

            int resultcolunm = 0;
            for (int i = 0 ; i < driverList.Count ; i++)
            {
                switch (driverList[i].OutputType)
                {
                    case ControlTYpe.Position:
                        
                        resultcolunm += 3;
                        break;

                    case ControlTYpe.OrientationX:
                        
                        resultcolunm += 3;
                        break;

                    case ControlTYpe.OrientationY:

                        resultcolunm += 3;
                        break;

                    case ControlTYpe.OrientationZ:

                        resultcolunm += 3;
                        break;

                    case ControlTYpe.Rotation:
                        
                        resultcolunm += 4;
                        break;
                }
            }

            DenseMatrix M_Output = new DenseMatrix(posecount, resultcolunm);
            //List<DenseMatrix> M_Output = new List<DenseMatrix>();

            M_Weight = new DenseMatrix(posecount, resultcolunm);

            for (int i = 0; i < posecount; i++)
            {
                int index = 0;
                
                for (int j = 0; j < driverList.Count; j++)
                {

                    switch (driverList[j].OutputType)
                    {
                        case ControlTYpe.Position:

                            resultitemlength = 3;
                            break;

                        case ControlTYpe.OrientationX:

                            resultitemlength = 3;
                            break;

                        case ControlTYpe.OrientationY:

                            resultitemlength = 3;
                            break;

                        case ControlTYpe.OrientationZ:

                            resultitemlength = 3;
                            break;

                        case ControlTYpe.Rotation:

                            resultitemlength = 4;
                            break;
                    }

                    for (int m = 0; m < resultitemlength; m++)
                    {
                        M_Output[i, index + m] = PoseList[i].output[j][m];
                    }

                    index = index + resultitemlength;

                }
            }

            Matrix<double> M_Similart_INV = M_Similart.Inverse();

            M_Weight = M_Similart_INV.Multiply(M_Output) as DenseMatrix;


            //Debug.Log(M_Similart);

        }
    }

    //计算当前结果
    public void CalculateCurrentOut()
    {
        if (MainController != null)
        {

            List<Vector4> ControllerInput = new List<Vector4>();


            foreach (GameObject item in MainController)
            {
                switch (MainControlType)
                {
                    case ControlTYpe.Position:
                        
                        ControllerInput.Add(RootBone.InverseTransformPoint(item.transform.position));                        

                        break;

                    case ControlTYpe.OrientationX:
                        
                            ControllerInput.Add(RootBone.InverseTransformDirection(item.transform.right));                        

                        break;

                    case ControlTYpe.OrientationY:

                        ControllerInput.Add(RootBone.InverseTransformDirection(item.transform.up));

                        break;

                    case ControlTYpe.OrientationZ:

                        ControllerInput.Add(RootBone.InverseTransformDirection(item.transform.forward));

                        break;

                    case ControlTYpe.Rotation:
                        
                            Quaternion q = Quaternion.Inverse(RootBone.rotation) * item.transform.rotation;
                            ControllerInput.Add(new Vector4(q.x, q.y, q.z, q.w));
                        
                        break;
                }
            }
            
            //当前Pose相似度Vector计算 1*posecount
            int posecount = PoseList.Count;
            DenseVector V_Input = new DenseVector(posecount);

            for (int i = 0; i < posecount; i++)
            {

                //V_Input[i] = Vector4.Distance(ControllerInput, PoseList[i].input);
                V_Input[i] = CalculateFanshu(ControllerInput, PoseList[i].input);

                //核函数 高斯
                //V_Input[i] = Gaussian(V_Input[i]);

            }

            //var M_result = V_Input.Multiply(M_Weight);
            var M_result = M_Weight.TransposeThisAndMultiply(V_Input);

            

            int index = 0;
            for (int i = 0; i < driverList.Count; i++)
            {
                Vector4 result = new Vector4();

                switch (driverList[i].OutputType)
                {
                    case ControlTYpe.Position:
                        resultitemlength = 3;

                        ///
                        for (int j = 0; j < resultitemlength; j++)
                        {
                            result[j] = (float)M_result[index + j];
                        }

                        index = index + resultitemlength;
                        ///

                        driverList[i].OutputObject.position = OutputReference.TransformPoint(result);
                        break;

                    case ControlTYpe.OrientationX:

                        //Output[i].position = OutputReference.TransformDirection(result);
                        break;

                    case ControlTYpe.OrientationY:

                        //Output[i].position = OutputReference.TransformDirection(result);
                        break;

                    case ControlTYpe.OrientationZ:

                        //Output[i].position = OutputReference.TransformDirection(result);
                        break;

                    case ControlTYpe.Rotation:

                        resultitemlength = 4;

                        ///
                        for (int j = 0; j < resultitemlength; j++)
                        {
                            result[j] = (float)M_result[index + j];
                        }

                        index = index + resultitemlength;


                        driverList[i].OutputObject.rotation = OutputReference.rotation * new Quaternion(result.x, result.y, result.z, result.w);
                        break;
                }
            }

        }
    }
}
