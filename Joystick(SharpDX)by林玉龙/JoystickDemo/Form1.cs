using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace JoystickDemo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Thread t1 = new Thread(TJsGet);
            t1.IsBackground = true;
            t1.Start();
        }

        SharpDX.DirectInput.Joystick curJoystick;
        delegate void SetMessageCallBack(TextBox txtIn, string MyMessage);
        private void SetMessage(TextBox txtIn, string MyMessageIn)
        {
            try
            {
                if (!MyMessageIn.EndsWith(Environment.NewLine))
                {
                    MyMessageIn += Environment.NewLine;//加上换行符
                }
                if (this.InvokeRequired)
                {
                    SetMessageCallBack tmpMessage = new SetMessageCallBack(SetMessage);
                    this.Invoke(tmpMessage, new object[] { txtIn, MyMessageIn });
                }
                else
                {
                    txtIn.Text = MyMessageIn;
                }
            }
            catch (Exception ex)
            {
                //线程时间太短，容易在关闭窗口时引起异常：
                //无法访问已释放的对象。对象名:“Form1”。
                string tmp = ex.Message;
            }
        }

        enum JoystickPViewHatInfo
        {
            PViewHat0 = 0, PViewHat4500 = 4500, PViewHat9000 = 9000, PViewHat13500 = 13500, PViewHat18000 = 18000
                , PViewHat22500 = 22500, PViewHat27000 = 27000, PViewHat31500 = 31500, PViewHat_Null = -1
        }
        /// <summary>
        /// 由于不同dll中的摇杆数据类型略有不同，因此都转为自己定义的再处理
        /// </summary>
        class JoystickInfoClass
        {
            //buttons
            public bool[] Buttons = new bool[128];
            //point of view controlls
            public JoystickPViewHatInfo[] PointOfViewControllers=new JoystickPViewHatInfo[4];
            //sliders
            public int[] Sliders = new int[2];
            public int[] VelocitySliders = new int[2];
            public int[] AccelerationSliders = new int[2];
            public int[] ForceSliders = new int[2];
            //axes
            public int X;//左右，左边小
            public int Y;//前后，前面小
            public int Z;
            public int RotationX;
            public int RotationY;
            public int RotationZ;
            public int VelocityX;
            public int VelocityY;
            public int VelocityZ;
            public int AngularVelocityX;
            public int AngularVelocityY;
            public int AngularVelocityZ;
            public int AccelerationX;
            public int AccelerationY;
            public int AccelerationZ;
            public int AngularAccelerationX;
            public int AngularAccelerationY;
            public int AngularAccelerationZ;
            public int ForceX;
            public int ForceY;
            public int ForceZ;
            public int TorqueX;
            public int TorqueY;
            public int TorqueZ;
        }

        string JoystickName = "";//支持最多1个手柄同时插入
        JoystickInfoClass m_JoystickInfoLast;
        JoystickInfoClass m_JoystickInfoNew;

        /// <summary>
        /// 判断一个类的两个实例的属性值和字段值是否完全一样
        /// </summary>
        /// <param name="obj1"></param>
        /// <param name="obj2"></param>
        /// <remarks>数组中只判断可强制转换为int[]的字段数组</remarks>
        /// <returns></returns>
        private bool ObjectEquel(object obj1, object obj2)
        {
            Type type1 = obj1.GetType();
            Type type2 = obj2.GetType();

            System.Reflection.PropertyInfo[] properties1 = type1.GetProperties();
            System.Reflection.PropertyInfo[] properties2 = type2.GetProperties();
            System.Reflection.FieldInfo[] field1 = type1.GetFields();
            System.Reflection.FieldInfo[] field2 = type2.GetFields();
            bool IsMatch = true;
            ///0 if they are value type
            ///1 if they are array
            ///2 if they are class
            ///3 if they are other type
            //判断所有属性值是否相同
            for (int i = 0; i < properties1.Length; i++)
            {
                string s = properties1[i].DeclaringType.Name;
                Type retp1type = properties1[i].GetType();
                var retp1 = properties1[i].GetValue(obj1, null);
                var retp2 = properties2[i].GetValue(obj2, null);
                if (retp1.ToString() != retp2.ToString())
                {
                    IsMatch = false;
                    break;
                }
            }
            //判断所有字段值是否相同
            for (int i = 0; i < field1.Length; i++)
            {
                var fValue1 = field1[i].GetValue(obj1);
                var fValue2 = field2[i].GetValue(obj2);
                bool isArray = fValue1.GetType().IsArray;
                bool isValue = fValue1.GetType().IsValueType;
                if (isArray)
                {
                    //只判断可强制转换为int[]的数组
                    try
                    {
                        int[] fv11 = (int[])fValue1;
                        int[] fv22 = (int[])fValue2;
                        for (int j = 0; j < fv11.Length; j++)
                        {
                            if (fv11[i]!=fv22[i])
                            {
                                IsMatch = false;
                                i = field1.Length;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
                if (isValue)
                {
                    if (fValue1.ToString() != fValue2.ToString())
                    {
                        IsMatch = false;
                        break;
                    }
                }
            }
            return IsMatch;
        }
        private bool CompareJoystickInfoClass(JoystickInfoClass J1, JoystickInfoClass J2)
        {
            bool isMatch = true;//默认一致
            if (!ObjectEquel(J1,J2))
            {
                //如果碰到某个字段或属性值不一致，直接返回false
                isMatch = false;
                return isMatch;
            }
            //数组类型的暂时不知道怎么判断，只能直接for判断
            for (int i = 0; i < J1.Buttons.Length; i++)
            {
                if (J1.Buttons[i] != J2.Buttons[i])
                {
                    isMatch = false;
                    break;
                }
            }
            for (int i = 0; i < J1.PointOfViewControllers.Length; i++)
            {
                if (J1.PointOfViewControllers[i] != J2.PointOfViewControllers[i])
                {
                    isMatch = false;
                    break;
                }
            }
            for (int i = 0; i < J1.Sliders.Length; i++)
            {
                if (J1.Sliders[i] != J2.Sliders[i])
                {
                    isMatch = false;
                    break;
                }
            }
            for (int i = 0; i < J1.VelocitySliders.Length; i++)
            {
                if (J1.VelocitySliders[i] != J2.VelocitySliders[i])
                {
                    isMatch = false;
                    break;
                }
            }
            for (int i = 0; i < J1.AccelerationSliders.Length; i++)
            {
                if (J1.AccelerationSliders[i] != J2.AccelerationSliders[i])
                {
                    isMatch = false;
                    break;
                }
            }
            for (int i = 0; i < J1.ForceSliders.Length; i++)
            {
                if (J1.ForceSliders[i] != J2.ForceSliders[i])
                {
                    isMatch = false;
                    break;
                }
            }
            return isMatch;
        }

        private void TJsListening()
        {
            MessageBox.Show("监听到手柄");
            m_JoystickInfoLast = new JoystickInfoClass();
            m_JoystickInfoNew = new JoystickInfoClass();
            for (int i = 0; i < m_JoystickInfoNew.Buttons.Length; i++)
            {
                m_JoystickInfoLast.Buttons[i] = false;//初始化时，所有按键均未按下
            }
            m_JoystickInfoLast.PointOfViewControllers[0] = JoystickPViewHatInfo.PViewHat_Null;
            try
            {
                while (true)
                {
                    //初始化新按键
                    for (int i = 0; i < m_JoystickInfoNew.Buttons.Length; i++)
                    {
                        m_JoystickInfoNew.Buttons[i] = false;//初始化时，所有按键均未按下
                    }
                    m_JoystickInfoNew.PointOfViewControllers[0] = JoystickPViewHatInfo.PViewHat_Null;

                    //如果手柄掉了，会引发异常，结束该线程
                    var CurJoyState = curJoystick.GetCurrentState();

                    //获取按键信息
                    //获取的按键名比厂家软件显示的小1
                    int ButtonsLen = CurJoyState.Buttons.Length;
                    for (int j = 0; j < ButtonsLen; j++)
                    {
                        if (Convert.ToInt32(CurJoyState.Buttons[j]) > 0)
                        {
                            m_JoystickInfoNew.Buttons[j] = true;//按下
                        }
                    }

                    //获取Point Of View Hat信息
                    //int PointOfViewLen= b.GetPointOfView().Length;
                    int PointOfViewLen = CurJoyState.PointOfViewControllers.Length;
                    for (int j = 0; j < PointOfViewLen; j++)
                    {
                        //数据范围是0,4500,9000,13500,18000,22500,27000,31500
                        //没有按的时候是-1
                        m_JoystickInfoNew.PointOfViewControllers[j] = (JoystickPViewHatInfo)Convert.ToInt32(CurJoyState.PointOfViewControllers[j]);
                    }

                    //获取Slider信息
                    int SliderLen = CurJoyState.Sliders.Length;
                    for (int j = 0; j < SliderLen; j++)
                    {
                        m_JoystickInfoNew.AccelerationSliders[j] = CurJoyState.AccelerationSliders[j];
                        m_JoystickInfoNew.ForceSliders[j] = CurJoyState.ForceSliders[j];
                        m_JoystickInfoNew.VelocitySliders[j] = CurJoyState.VelocitySliders[j];
                        m_JoystickInfoNew.Sliders[j] = CurJoyState.Sliders[j];
                    }

                    //获取摇杆信息
                    m_JoystickInfoNew.X = CurJoyState.X;
                    m_JoystickInfoNew.Y = CurJoyState.Y;
                    m_JoystickInfoNew.Z = CurJoyState.Z;
                    m_JoystickInfoNew.VelocityX = CurJoyState.VelocityX;
                    m_JoystickInfoNew.VelocityY = CurJoyState.VelocityY;
                    m_JoystickInfoNew.VelocityZ = CurJoyState.VelocityZ;
                    m_JoystickInfoNew.TorqueX = CurJoyState.TorqueX;
                    m_JoystickInfoNew.TorqueY = CurJoyState.TorqueY;
                    m_JoystickInfoNew.TorqueZ = CurJoyState.TorqueZ;
                    m_JoystickInfoNew.RotationX = CurJoyState.RotationX;
                    m_JoystickInfoNew.RotationY = CurJoyState.RotationY;
                    m_JoystickInfoNew.RotationZ = CurJoyState.RotationZ;
                    m_JoystickInfoNew.ForceX = CurJoyState.ForceX;
                    m_JoystickInfoNew.ForceY = CurJoyState.ForceY;
                    m_JoystickInfoNew.ForceZ = CurJoyState.ForceZ;
                    m_JoystickInfoNew.AngularVelocityX = CurJoyState.AngularVelocityX;
                    m_JoystickInfoNew.AngularVelocityY = CurJoyState.AngularVelocityY;
                    m_JoystickInfoNew.AngularVelocityZ = CurJoyState.AngularVelocityZ;
                    m_JoystickInfoNew.AngularAccelerationX = CurJoyState.AngularAccelerationX;
                    m_JoystickInfoNew.AngularAccelerationY = CurJoyState.AngularAccelerationY;
                    m_JoystickInfoNew.AngularAccelerationZ = CurJoyState.AngularAccelerationZ;
                    m_JoystickInfoNew.AccelerationX = CurJoyState.AccelerationX;
                    m_JoystickInfoNew.AccelerationY = CurJoyState.AccelerationY;
                    m_JoystickInfoNew.AccelerationZ = CurJoyState.AccelerationZ;
                    bool bIsSameJs = CompareJoystickInfoClass(m_JoystickInfoLast, m_JoystickInfoNew);
                }
            }
            catch (Exception ex)
            {
                curJoystick.Unacquire();
                JoystickName = "";
            }
        }

        /// <summary>
        /// 不间断尝试重新连接手柄
        /// </summary>
        private void TJsGet()
        {
            int JoystickCount = 0;
            var dirInput = new SharpDX.DirectInput.DirectInput();
            while (true)
            {
                if (JoystickName != "")
                {
                    //已经接入最大数量的手柄了
                    //10s检测一次手柄掉线，如果未掉线，则跳过
                    Thread.Sleep(10000);
                    continue;
                }
                var allDevices = dirInput.GetDevices();
                foreach (var item in allDevices)
                {
                    if (SharpDX.DirectInput.DeviceType.Joystick == item.Type)
                    {
                        //记录新建线程的手柄名称
                        if (JoystickName == item.ProductName)
                        {
                            //说明已经接入这个手柄了
                        }
                        else
                        {
                            JoystickName = item.ProductName;
                            curJoystick = new SharpDX.DirectInput.Joystick(dirInput, item.InstanceGuid);
                            curJoystick.Properties.AxisMode = SharpDX.DirectInput.DeviceAxisMode.Absolute;
                            curJoystick.Acquire();
                            JoystickCount++;
                            Thread t1 = new Thread(TJsListening);
                            t1.IsBackground = true;
                            t1.Start();
                        }
                        break;
                        //curJoystick.Unacquire();//释放手柄
                    }
                }
                if (JoystickCount == 0)
                {
                    MessageBox.Show("手柄数量 " + JoystickCount.ToString());
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

    }
}
