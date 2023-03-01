using System;
using System.Numerics;

namespace EllipticCurve
{
    public class EllipticCurve
    {
        public int a = 0;
        public BigInteger p = BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007908834671663");
        public static BigInteger n = BigInteger.Parse("115792089237316195423570985008687907852837564279074904382605163141518161494337");
        public static BigInteger x_G = BigInteger.Parse("55066263022277343669578718895168534326250603453777594175500187360389116729240");
        public static BigInteger y_G = BigInteger.Parse("32670510020758816978083085130507043184471273380659243275938904335757337482424");

        public BigInteger correctNum(BigInteger num)
        {
            while (num < 0)
                num += this.p;
            return num % this.p;
        }
    }

    public class EllipticCurvePoint : EllipticCurve
    {
        public BigInteger x;
        public BigInteger y;

        public EllipticCurvePoint()
        {

        }

        public EllipticCurvePoint(BigInteger x, BigInteger y)
        {
            this.x = x;
            this.y = y;
        }

        private BigInteger binPow(BigInteger num, BigInteger pow)
        {
            BigInteger res = 1;

            while (pow != 0)
            {
                if ((pow & 1) == 1)
                    res = res * num % p;
                num = num * num % p;
                pow >>= 1;
            }
            return res;
        }

        private BigInteger inv(BigInteger num)
        {
            return binPow(num, p - 2);
        }

        public EllipticCurvePoint addingPoint(EllipticCurvePoint point)
        {
            BigInteger s = (correctNum(point.y - this.y) * inv(point.x - this.x)) % p;
            BigInteger x = correctNum(((s * s) % p) - this.x - point.x);
            BigInteger y = correctNum(((s * (this.x - x)) % p) - this.y);

            return new EllipticCurvePoint(x, y);
        }

        public EllipticCurvePoint doublingPoint()
        {
            BigInteger s = ((3 * ((this.x * this.x) % p) + a) * inv(2 * this.y)) % p;
            BigInteger x = correctNum(((s * s) % p) - ((2 * this.x) % p));
            BigInteger y = correctNum(((s * (this.x - x)) % p) - this.y);

            return new EllipticCurvePoint(x, y);
        }

        public EllipticCurvePoint scalMultNumByPointEC(BigInteger n)
        {
            EllipticCurvePoint res = null;
            EllipticCurvePoint point = this;

            if (n == 0)
            {
                throw new Exception("Получился нулевой элемент группы.");
            }
            else
            {
                if ((n & BigInteger.One) == BigInteger.One)
                    res = this;
                n >>= 1;
                while (n != 0)
                {
                    point = point.doublingPoint();
                    if ((n & BigInteger.One) == BigInteger.One)
                    {
                        if (res == null)
                            res = point;
                        else res = res.addingPoint(point);
                    }
                    n >>= 1;
                }

                return res;
            }
        }

        public EllipticCurvePoint inversePoint()
        {
            this.y = correctNum(-this.y);
            return this;
        }
    }
}