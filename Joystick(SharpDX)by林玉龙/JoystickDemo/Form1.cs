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
            var dirInput = new SharpDX.DirectInput.DirectInput();
            var typeJoystick = SharpDX.DirectInput.DeviceType.Joystick;
            var allDevices = dirInput.GetDevices();
            bool isGetJoystick = false;
            foreach (var item in allDevices)
            {
                if (typeJoystick == item.Type)
                {
                    curJoystick = new SharpDX.DirectInput.Joystick(dirInput, item.InstanceGuid);
                    curJoystick.Acquire();
                    isGetJoystick = true;
                    Thread t1 = new Thread(joyListening);
                    t1.IsBackground = true;
                    t1.Start();
                }
            }
            if (!isGetJoystick)
            {
                MessageBox.Show("没有插入手柄");
            }
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

        private void joyListening()
        {
            MessageBox.Show("监听到手柄");
            while (true)
            {
                var joys = curJoystick.GetCurrentState();
                SetMessage(textBox1, joys.ToString());
                int x = joys.X;
                int y = joys.Y;
                bool[] joyButton = joys.Buttons;
                int[] joySliders = joys.Sliders;
                int z = joys.Z;
                Thread.Sleep(100);
            }
        }
    }
}
