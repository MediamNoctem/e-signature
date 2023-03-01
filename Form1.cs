using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Diagnostics;
using System.Numerics;
using System.IO;
using System.Text;

namespace Coursework
{
    public partial class Form1 : Form
    {
        private int height, width, beginningOfImage;
        private DigitalSignature ds = new DigitalSignature();
        private byte[] imageOriginal;
        private BigInteger d;
        
        public Form1()
        {
            InitializeComponent();
        }

        private string addZero(string str, int num)
        {
            while (str.Length % num != 0)
                str = "0" + str;
            return str;
        }

        private string BeginningOfImage(byte b1, byte b2, byte b3, byte b4)
        {
            return Convert.ToString(b1, 16) + Convert.ToString(b2, 16) + Convert.ToString(b3, 16) + Convert.ToString(b4, 16);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "(*.bmp)|*.bmp";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                pictureBox1.Image = Image.FromFile(ofd.FileName);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null)
                MessageBox.Show("Выберите изображение!", "Ошибка!");
            else
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                Image image = pictureBox1.Image;
                height = image.Height;
                width = image.Width;

                MemoryStream memoryStream = new MemoryStream();
                image.Save(memoryStream, ImageFormat.Bmp);
                imageOriginal = memoryStream.ToArray();

                beginningOfImage = Convert.ToInt32(BeginningOfImage(imageOriginal[13], imageOriginal[12], imageOriginal[11], imageOriginal[10]), 16);

                if (imageOriginal[28] == 32)
                {
                    if (textBox1.Text == "")
                        MessageBox.Show("Введите ключ подписи!", "Ошибка!");
                    else
                    {
                        d = BigInteger.Parse(textBox1.Text);

                        // Формирование ключа проверки подписи.
                        /*EllipticCurve.EllipticCurvePoint Q = ds.G.scalMultNumByPointEC(d);
                        StreamWriter f = new StreamWriter("signatureVerificationKey.txt");
                        f.WriteLine("X: " + Q.x.ToString() + "\nY: " + Q.y.ToString());
                        f.Close();*/

                        BigInteger signature = BigInteger.Parse(ds.ToFormDigitalSignature(image, d, beginningOfImage, height, width));

                        textBox2.Text = signature.ToString("X");
                        stopwatch.Stop();
                        label9.Text = "Время формирования подписи: " + ((float)(stopwatch.ElapsedMilliseconds / 1000.0f)).ToString() + " c";
                    }
                }
                else
                    MessageBox.Show("Формат пикселя не WRGB!");
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            pictureBox2.Image = null;
            textBox3.Text = "";
            textBox6.Text = "";
            textBox4.Text = "";
            textBox5.Text = "";
            label10.Text = "";
        }

        private void button6_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = null;
            textBox1.Text = "";
            textBox2.Text = "";
            label9.Text = "";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "(*.bmp)|*.bmp";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                pictureBox2.Image = Image.FromFile(ofd.FileName);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (pictureBox2.Image == null)
                MessageBox.Show("Выберите изображение!", "Ошибка!");
            else
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                Image image = pictureBox2.Image;
                height = image.Height;
                width = image.Width;

                MemoryStream memoryStream = new MemoryStream();
                image.Save(memoryStream, ImageFormat.Bmp);
                imageOriginal = memoryStream.ToArray();

                if (textBox3.Text == "" || textBox6.Text == "")
                    MessageBox.Show("Введите ключ проверки подписи!", "Ошибка!");
                else
                {
                    if (textBox4.Text == "")
                        MessageBox.Show("Введите ЭЦП!", "Ошибка!");
                    else
                    {
                        beginningOfImage = Convert.ToInt32(BeginningOfImage(imageOriginal[13], imageOriginal[12], imageOriginal[11], imageOriginal[10]), 16);

                        EllipticCurve.EllipticCurvePoint Q = new EllipticCurve.EllipticCurvePoint(BigInteger.Parse(textBox3.Text), BigInteger.Parse(textBox6.Text));
                        BigInteger sig = BigInteger.Parse(textBox4.Text, System.Globalization.NumberStyles.HexNumber);
                        string signature = sig.ToString();
                        signature = addZero(signature, 256 * 2);
                        bool res = ds.ToVerifyDigitalSignature(image, signature, Q, beginningOfImage, height, width);
                        textBox5.Text = res.ToString();
                        stopwatch.Stop();
                        label10.Text = "Время проверки подлинности: " + ((float)(stopwatch.ElapsedMilliseconds / 1000.0f)).ToString() + " c";
                    }
                }
            }
        }
    }
    public class DigitalSignature
    {
        Gost_34_11_2018 G256 = new Gost_34_11_2018(256);
        EllipticCurve.EllipticCurvePoint C;
        public EllipticCurve.EllipticCurvePoint G = new EllipticCurve.EllipticCurvePoint(EllipticCurve.EllipticCurve.x_G, EllipticCurve.EllipticCurve.y_G);
        BigInteger n = EllipticCurve.EllipticCurve.n;
        BigInteger e;

        public string ToFormDigitalSignature(Image image, BigInteger signatureKey, int beginningOfImage, int height, int width)
        {
            MemoryStream memoryStream = new MemoryStream();
            image.Save(memoryStream, ImageFormat.Bmp);
            byte[] img = memoryStream.ToArray();

            byte[] hash = G256.GetHash(img);

            BigInteger alpha = 0, k, r, s;
            Random rnd = new Random();

            for (int i = 0; i < hash.Length; i++)
                alpha += hash[i] * (BigInteger)(Math.Pow(2, i));

            e = alpha % n;
            if (e == 0) e = 1;

            while (true)
            {
                k = rnd.Next(0, n);
                if (k == 0 || k == n) continue;

                C = G.scalMultNumByPointEC(k);
                r = C.x % n;
                if (r == 0) continue;
                s = (r * signatureKey + k * e) % n;
                if (s == 0) continue;
                break;
            }

            string r_vector = r.ToBinaryString(256);
            string s_vector = s.ToBinaryString(256);

            return r_vector + s_vector;
        }

        BigInteger[] gcdex(BigInteger a, BigInteger b)
        {
            if (a == 0)
                return new BigInteger[] { b, 0, 1 };
            BigInteger[] gcd = gcdex(b % a, a);
            return new BigInteger[] { gcd[0], gcd[2] - (b / a) * gcd[1], gcd[1] };
        }

        BigInteger invmod(BigInteger a, BigInteger m)
        {
            BigInteger[] g = gcdex(a, m);
            if (g[0] > 1)
                return BigInteger.Zero;
            else
                return (g[1] % m + m) % m;
        }

        public bool ToVerifyDigitalSignature(Image image, string digitalSignature, EllipticCurve.EllipticCurvePoint signatureVerificationKey, int beginningOfImage, int height, int width)
        {
            BigInteger r = digitalSignature.Substring(0, 256).FromBinary();
            BigInteger s = digitalSignature.Substring(256).FromBinary();

            if ((!(r > 0 && r < n)) || (!(s > 0 && s < n)))
                return false;

            MemoryStream memoryStream = new MemoryStream();
            image.Save(memoryStream, ImageFormat.Bmp);
            byte[] img = memoryStream.ToArray();

            byte[] hash = G256.GetHash(img);

            BigInteger alpha = 0;

            for (int k = 0; k < hash.Length; k++)
                alpha += hash[k] * (BigInteger)Math.Pow(2, k);

            e = alpha % n;
            if (e == 0) e = 1;

            BigInteger v = invmod(e, n);
            BigInteger z1, z2;

            z1 = (s * v) % n;
            z2 = ((n - r) * v) % n;

            C = G.scalMultNumByPointEC(z1).addingPoint(signatureVerificationKey.scalMultNumByPointEC(z2));

            BigInteger R = C.x % n;

            if (R == r)
                return true;
            
            return false;
        }
    }

    public static class RandomExtension
    {
        public static BigInteger Next(this Random random, BigInteger minValue, BigInteger maxValue)
        {
            int number_digits_min = minValue.ToString().Length, number_digits_max = maxValue.ToString().Length;
            int number_digits_in_num = random.Next(number_digits_min, number_digits_max);
            string num = "";
            int digit;

            for (int i = 0; i < number_digits_in_num; i++)
            {
                digit = random.Next(0, 10);
                num += digit.ToString();
            }
            return BigInteger.Parse(num);
        }
    }

    public static class BigIntegerExtension
    {
        public static string ToBinaryString(this BigInteger bigint, int maxNumOfDigits)
        {
            byte[] bytes = bigint.ToByteArray();
            int idx = bytes.Length - 1;

            StringBuilder base2 = new StringBuilder(bytes.Length * 8);
            string binary = Convert.ToString(bytes[idx], 2);

            base2.Append(binary);
            
            for (idx--; idx >= 0; idx--)
                base2.Append(Convert.ToString(bytes[idx], 2).PadLeft(8, '0'));
            
            int diff = maxNumOfDigits - base2.Length;

            string zero = "";

            for (int i = 0; i < diff; i++)
                zero += "0";

            if (diff < 0)
                base2.Remove(0, Math.Abs(diff));

            return zero + base2.ToString();
        }
    }

    public static class StringExtension
    {
        public static BigInteger FromBinary(this string input)
        {
            BigInteger big = new BigInteger();
            foreach (var c in input)
            {
                big <<= 1;
                big += c - '0';
            }

            return big;
        }
    }
}