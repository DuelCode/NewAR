using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using jp.nyatla.nyartoolkit.cs.core;

namespace NyARToolkitCSUtils.Common
{
    public enum LayoutDirType : int
    {
        ldtUp = 0,
        ldtRight = 1,
        ldtDown = 2,
        ldtLeft = 3,
        ldtNone = 4
    }

    public enum RotateDirType : int
    {
        rdtNone = 0,
        rdtLeft = 1,
        rdtRight = 2,
    }

    public class MarkerInfo
    {
        public int MarkerID;
        public NyARIntPoint2d[] Vertex; // 顶点坐标
        public NyARIntPoint2d Center; // 中心点坐标
        public LayoutDirType CurLayoutDir = LayoutDirType.ldtNone; // 标签朝向
        public RotateDirType CurRotateDir = RotateDirType.rdtNone; // 旋转方向
        public double Rotate = 0; // 旋转值
        public double Confidence = 0;

        public MarkerInfo(int nMarkerID)
        {
            this.MarkerID = nMarkerID;
        }

        public MarkerInfo(int nMarkerID, NyARIntPoint2d[] oPoints, NyARIntPoint2d oCenterPoint, double dConfidence)
        {
            this.MarkerID = nMarkerID;
            this.Vertex = oPoints;
            this.Center = oCenterPoint;
            this.Confidence = dConfidence;
            this.CalcDir();
            this.CalcDegree();
        }

        public void Update(NyARIntPoint2d[] oPoints, NyARIntPoint2d oCenterPoint, double dConfidence)
        {
            this.Vertex = oPoints;
            this.Center = oCenterPoint;
            this.Confidence = dConfidence;
            this.CalcDir();
            this.CalcDegree();
        }

        private void CalcDir()
        {
            int nPointMinXCount_0X = 0;
            if (Vertex[0].x < Vertex[1].x)
                nPointMinXCount_0X++;
            if (Vertex[0].x < Vertex[2].x)
                nPointMinXCount_0X++;
            if (Vertex[0].x < Vertex[3].x)
                nPointMinXCount_0X++;

            int nPointMinYCount_0Y = 0;
            if (Vertex[0].y < Vertex[1].y)
                nPointMinYCount_0Y++;
            if (Vertex[0].y < Vertex[2].y)
                nPointMinYCount_0Y++;
            if (Vertex[0].y < Vertex[3].y)
                nPointMinYCount_0Y++;

            if (nPointMinXCount_0X >= 2 && nPointMinYCount_0Y >= 2)
                this.CurLayoutDir = LayoutDirType.ldtUp;
            else if (nPointMinXCount_0X >= 2 && nPointMinYCount_0Y < 2)
                this.CurLayoutDir = LayoutDirType.ldtLeft;
            else if (nPointMinXCount_0X < 2 && nPointMinYCount_0Y >= 2)
                this.CurLayoutDir = LayoutDirType.ldtRight;
            else if (nPointMinXCount_0X < 2 && nPointMinYCount_0Y < 2)
                this.CurLayoutDir = LayoutDirType.ldtDown;
            else
                this.CurLayoutDir = LayoutDirType.ldtNone;
        }

        private double ConvertToDegree(double dRadValue)
        {
            return dRadValue * 180.0 / Math.PI;
        }

        /// <summary>
        /// A: Point[0]
        /// B: Point[1]
        /// C: Point[2]
        /// D: Point[3]
        /// 
        /// Up ****************
        /// A B
        /// D C
        /// 
        /// Left***************
        /// B C
        /// A D
        /// 
        /// Down***************
        /// C D
        /// B A
        /// 
        /// Right**************
        /// D A
        /// C B
        /// 
        /// </summary>
        private void CalcDegree()
        {
            if (this.Vertex == null || this.Vertex.Length < 4) return;

            Point oPointA = new Point(this.Vertex[0].x, this.Vertex[0].y);
            Point oPointB = new Point(this.Vertex[1].x, this.Vertex[1].y);
            Point oPointC = new Point(this.Vertex[2].x, this.Vertex[2].y);
            Point oPointD = new Point(this.Vertex[3].x, this.Vertex[3].y);

            if (this.CurLayoutDir == LayoutDirType.ldtUp)
            {
                #region
                double dDAY = oPointD.Y - oPointA.Y;
                double dCBY = oPointC.Y - oPointB.Y;
                double dIntervalX = (oPointB.X - oPointA.X) / 2d + (oPointC.X - oPointD.X) / 2d;
                double dIntervalY = (dCBY + dDAY) / 2d;
                if (dDAY < dCBY)
                {
                    this.CurRotateDir = RotateDirType.rdtLeft;

                    // 通过中心点、计算偏移角度 --- 误差在0-5度之间。  精确度较低
                    //double dRaduis = dCBY/2d;
                    //double dActual = (oPointC.X + oPointB.X)/2d - this.Center.x;
                    //this.Rotate = -ConvertToDegree(Math.Acos(dActual/dRaduis));
                    this.Rotate = -ConvertToDegree(Math.Acos(dIntervalX / dIntervalY));
                }
                else if (dDAY > dCBY)
                {
                    this.CurRotateDir = RotateDirType.rdtRight;

                    // 通过中心点、计算偏移角度 --- 误差在0-5度之间。  精确度较低
                    //double dRaduis = dDAY/2d;
                    //double dActual = this.Center.x - (oPointA.X + oPointD.X)/2d;
                    //this.Rotate = ConvertToDegree(Math.Acos(dActual / dRaduis));
                    this.Rotate = ConvertToDegree(Math.Acos(dIntervalX / dIntervalY));
                }
                else
                {
                    this.CurRotateDir = RotateDirType.rdtNone;
                    this.Rotate = 0;
                }
                #endregion
            }
            else if (this.CurLayoutDir == LayoutDirType.ldtLeft)
            {
                #region
                double dABY = oPointA.Y - oPointB.Y;
                double dDCY = oPointD.Y - oPointC.Y;
                double dIntervalX = (oPointC.X - oPointB.X) / 2d + (oPointD.X - oPointA.X) / 2d;
                double dIntervalY = (dABY + dDCY) / 2d;

                if (dABY < dDCY)
                {
                    this.CurRotateDir = RotateDirType.rdtLeft;
                    this.Rotate = -ConvertToDegree(Math.Acos(dIntervalX / dIntervalY));
                }
                else if (dABY > dDCY)
                {
                    this.CurRotateDir = RotateDirType.rdtRight;
                    this.Rotate = ConvertToDegree(Math.Acos(dIntervalX / dIntervalY));
                }
                else
                {
                    this.CurRotateDir = RotateDirType.rdtNone;
                    this.Rotate = 0;
                }
                #endregion
            }
            else if (this.CurLayoutDir == LayoutDirType.ldtRight)
            {
                #region
                double dCDY = oPointC.Y - oPointD.Y;
                double dBAY = oPointB.Y - oPointA.Y;
                double dIntervalX = (oPointA.X - oPointD.X) / 2d + (oPointB.X - oPointC.X) / 2d;
                double dIntervalY = (dBAY + dCDY) / 2d;
                if (dCDY < dBAY)
                {
                    this.CurRotateDir = RotateDirType.rdtLeft;
                    this.Rotate = -ConvertToDegree(Math.Acos(dIntervalX / dIntervalY));
                }
                else if (dCDY > dBAY)
                {
                    this.CurRotateDir = RotateDirType.rdtRight;
                    this.Rotate = ConvertToDegree(Math.Acos(dIntervalX / dIntervalY));
                }
                else
                {
                    this.CurRotateDir = RotateDirType.rdtNone;
                    this.Rotate = 0;
                }
                #endregion
            }
            else if (this.CurLayoutDir == LayoutDirType.ldtDown)
            {
                #region
                double dBCY = oPointB.Y - oPointC.Y;
                double dADY = oPointA.Y - oPointD.Y;
                double dIntervalX = (oPointA.X - oPointB.X) / 2d + (oPointD.X - oPointC.X) / 2d;
                double dIntervalY = (dADY + dBCY) / 2d;
                if (dBCY < dADY)
                {
                    this.CurRotateDir = RotateDirType.rdtLeft;
                    this.Rotate = -ConvertToDegree(Math.Acos(dIntervalX / dIntervalY));
                }
                else if (dBCY > dADY)
                {
                    this.CurRotateDir = RotateDirType.rdtRight;
                    this.Rotate = ConvertToDegree(Math.Acos(dIntervalX / dIntervalY));
                }
                else
                {
                    this.CurRotateDir = RotateDirType.rdtNone;
                    this.Rotate = 0;
                }
                #endregion
            }
        }
    }
}
