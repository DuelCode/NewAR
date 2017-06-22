/* 
 * PROJECT: NyARToolkitCS
 * --------------------------------------------------------------------------------
 *
 * The NyARToolkitCS is C# edition NyARToolKit class library.
 * Copyright (C)2008-2012 Ryo Iizuka
 *
 * This work is based on the ARToolKit developed by
 *   Hirokazu Kato
 *   Mark Billinghurst
 *   HITLab, University of Washington, Seattle
 * http://www.hitl.washington.edu/artoolkit/
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as publishe
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * For further information please contact.
 *	http://nyatla.jp/nyatoolkit/
 *	<airmail(at)ebony.plala.or.jp> or <nyatla(at)nyatla.jp>
 * 
 */
using jp.nyatla.nyartoolkit.cs.core;
using jp.nyatla.nyartoolkit.cs.markersystem.utils;
using System.IO;
using System;
using System.Diagnostics;
namespace jp.nyatla.nyartoolkit.cs.markersystem
{
    /**
     * このクラスは、マーカベースARの制御クラスです。
     * 複数のARマーカとNyIDの検出情報の管理機能、撮影画像の取得機能を提供します。
     * このクラスは、ARToolKit固有の座標系を出力します。他の座標系を出力するときには、継承クラスで変換してください。
     * レンダリングシステム毎にクラスを派生させて使います。Javaの場合には、OpenGL用の{@link NyARGlMarkerSystem}クラスがあります。
     */
    public class NyARMarkerSystem : NyARSingleCameraSystem
    {
        public const int THLESHOLD_AUTO = 0x7fffffff;

        public const int LOST_DELAY_DEFAULT = 5;

        private const int MASK_IDTYPE = 0x7ffff000;
        private const int MASK_IDNUM = 0x00000fff;
        private const int IDTYPE_ARTK = 0x00000000;
        private const int IDTYPE_NYID = 0x00001000;
        private const int IDTYPE_PSID = 0x00002000;

        protected INyARMarkerSystemSquareDetect _sqdetect;
        private int _last_gs_th;
        private int _bin_threshold = THLESHOLD_AUTO;
        private TrackingList _tracking_list;
        private ARMarkerList _armk_list;
        private NyIdList _idmk_list;
        private ARPlayCardList _psmk_list;
        private int lost_th = 5;
        private INyARTransMat _transmat;
        private const int INITIAL_MARKER_STACK_SIZE = 10;

        public NyARMarkerSystem(INyARMarkerSystemConfig i_config)
            : base(i_config.getNyARSingleCameraView())
        {
            this._sqdetect = new SquareDetect(i_config);
            this._hist_th = i_config.createAutoThresholdArgorism();

            this._armk_list = new ARMarkerList();
            this._idmk_list = new NyIdList();
            this._psmk_list = new ARPlayCardList();
            this._tracking_list = new TrackingList();
            this._transmat = i_config.createTransmatAlgorism();
            this._on_sq_handler = new OnSquareDetect(this._view.getARParam(), this._armk_list, this._idmk_list, this._psmk_list, this._tracking_list, INITIAL_MARKER_STACK_SIZE);
        }

        public int addNyIdMarker(long i_id, double i_marker_size)
        {
            return this.addNyIdMarker(i_id, i_id, i_marker_size);
        }
       
        public int addNyIdMarker(long i_id_s, long i_id_e, double i_marker_size)
        {
            NyIdList.Item target = new NyIdList.Item(i_id_s, i_id_e, i_marker_size);
            this._idmk_list.Add(target);
            this._tracking_list.add(target);
            this._on_sq_handler.setMaxDetectMarkerCapacity(this._tracking_list.Count);
            return (this._idmk_list.Count - 1) | IDTYPE_NYID;
        }
       
        public int addPsARPlayCard(int i_id_s, int i_id_e, double i_marker_size)
        {
            Debug.Assert(i_id_s > 0 && i_id_s <= 6);
            Debug.Assert(i_id_e > 0 && i_id_e <= 6);
            ARPlayCardList.Item target = new ARPlayCardList.Item(i_id_s, i_id_e, i_marker_size);
            this._psmk_list.Add(target);
            this._tracking_list.add(target);
            this._on_sq_handler.setMaxDetectMarkerCapacity(this._tracking_list.Count);
            return (this._psmk_list.Count - 1) | IDTYPE_PSID;
        }
       
        public int addPsARPlayCard(int i_id, double i_marker_size)
        {
            return this.addPsARPlayCard(i_id, i_id, i_marker_size);
        }
       
        public int addARMarker(NyARCode i_code, int i_patt_edge_percentage, double i_marker_size)
        {
            ARMarkerList.Item target = new ARMarkerList.Item(i_code, i_patt_edge_percentage, i_marker_size);
            this._armk_list.add(target);
            this._tracking_list.add(target);
            this._on_sq_handler.setMaxDetectMarkerCapacity(this._tracking_list.Count);
            return (this._armk_list.Count - 1) | IDTYPE_ARTK;
        }
      
        public int addARMarker(Stream i_stream, int i_patt_resolution, int i_patt_edge_percentage, double i_marker_size)
        {
            NyARCode c = NyARCode.loadFromARPattFile(i_stream, i_patt_resolution, i_patt_resolution);
            return this.addARMarker(c, i_patt_edge_percentage, i_marker_size);
        }
       
        public int addARMarker(String i_file_name, int i_patt_resolution, int i_patt_edge_percentage, double i_marker_size)
        {
            try
            {
                NyARCode c = NyARCode.loadFromARPattFile(File.OpenRead(i_file_name), i_patt_resolution, i_patt_resolution);
                return this.addARMarker(c, i_patt_edge_percentage, i_marker_size);
            }
            catch (Exception e)
            {
                throw new NyARRuntimeException(e);
            }
        }

        public int addARMarker(INyARRgbRaster i_raster, int i_patt_resolution, int i_patt_edge_percentage,
            double i_marker_size)
        {
            NyARCode c = new NyARCode(i_patt_resolution, i_patt_resolution);
            NyARIntSize s = i_raster.getSize();
            INyARPerspectiveCopy pc = (INyARPerspectiveCopy) i_raster.createInterface(typeof (INyARPerspectiveCopy));
            INyARRgbRaster tr = NyARRgbRaster.createInstance(i_patt_resolution, i_patt_resolution);
            pc.copyPatt(0, 0, s.w, 0, s.w, s.h, 0, s.h, i_patt_edge_percentage, i_patt_edge_percentage, 4, tr);
            c.setRaster(tr);
            return this.addARMarker(c, i_patt_edge_percentage, i_marker_size);
        }

        public bool isExist(int i_id)
        {
            return this.getLife(i_id) > 0;
        }

        public double getConfidence(int i_id)
        {
            if ((i_id & MASK_IDTYPE) == IDTYPE_ARTK)
            {
                return this._armk_list[i_id & MASK_IDNUM].cf;
            }
            throw new NyARRuntimeException();
        }

        public long getNyId(int i_id)
        {
            if ((i_id & MASK_IDTYPE) == IDTYPE_NYID)
            {
                return this._idmk_list[i_id & MASK_IDNUM].nyid;
            }
            throw new NyARRuntimeException();
        }

        public int getCurrentThreshold()
        {
            return this._last_gs_th;
        }

        public long getLife(int i_id)
        {
            switch (i_id & MASK_IDTYPE)
            {
                case IDTYPE_ARTK:
                    return this._armk_list[i_id & MASK_IDNUM].life;
                case IDTYPE_NYID:
                    return this._idmk_list[i_id & MASK_IDNUM].life;
                case IDTYPE_PSID:
                    return this._psmk_list[i_id & MASK_IDNUM].life;
                default:
                    throw new NyARRuntimeException();
            }
        }

        public long getLostCount(int i_id)
        {
            switch (i_id & MASK_IDTYPE)
            {
                case IDTYPE_ARTK:
                    return this._armk_list[i_id & MASK_IDNUM].lost_count;
                case IDTYPE_NYID:
                    return this._idmk_list[i_id & MASK_IDNUM].lost_count;
                case IDTYPE_PSID:
                    return this._psmk_list[i_id & MASK_IDNUM].lost_count;
                default:
                    throw new NyARRuntimeException();
            }
        }

        public NyARDoublePoint3d getPlanePos(int i_id, int i_x, int i_y, NyARDoublePoint3d i_out)
        {
            this._view.getFrustum().unProjectOnMatrix(i_x, i_y, this.getTransformMatrix(i_id), i_out);
            return i_out;
        }
        private NyARDoublePoint3d _wk_3dpos = new NyARDoublePoint3d();

        public NyARDoublePoint2d getScreenPos(int i_id, double i_x, double i_y, double i_z, NyARDoublePoint2d i_out)
        {
            NyARDoublePoint3d _wk_3dpos = this._wk_3dpos;
            this.getTransformMatrix(i_id).transform3d(i_x, i_y, i_z, _wk_3dpos);
            this._view.getFrustum().project(_wk_3dpos, i_out);
            return i_out;
        }
        private NyARDoublePoint3d[] __pos3d = NyARDoublePoint3d.createArray(4);
        private NyARDoublePoint2d[] __pos2d = NyARDoublePoint2d.createArray(4);

        public INyARRgbRaster getPlaneImage(int i_id, NyARSensor i_sensor, double i_x1, double i_y1, double i_x2,
            double i_y2, double i_x3, double i_y3, double i_x4, double i_y4, INyARRgbRaster i_raster)
        {
            NyARDoublePoint3d[] pos = this.__pos3d;
            NyARDoublePoint2d[] pos2 = this.__pos2d;
            NyARDoubleMatrix44 tmat = this.getTransformMatrix(i_id);
            tmat.transform3d(i_x1, i_y1, 0, pos[1]);
            tmat.transform3d(i_x2, i_y2, 0, pos[0]);
            tmat.transform3d(i_x3, i_y3, 0, pos[3]);
            tmat.transform3d(i_x4, i_y4, 0, pos[2]);
            for (int i = 3; i >= 0; i--)
            {
                this._view.getFrustum().project(pos[i], pos2[i]);
            }
            return i_sensor.getPerspectiveImage(pos2[0].x, pos2[0].y, pos2[1].x, pos2[1].y, pos2[2].x, pos2[2].y,
                pos2[3].x, pos2[3].y, i_raster);
        }

        public INyARRgbRaster getPlaneImage(int i_id, NyARSensor i_sensor, double i_l, double i_t, double i_w,
            double i_h, INyARRgbRaster i_raster)
        {
            return this.getPlaneImage(i_id, i_sensor, i_l + i_w - 1, i_t + i_h - 1, i_l, i_t + i_h - 1, i_l, i_t,
                i_l + i_w - 1, i_t, i_raster);
        }

        public NyARDoubleMatrix44 getTransformMatrix(int i_id)
        {
            switch (i_id & MASK_IDTYPE)
            {
                case IDTYPE_ARTK:
                    return this._armk_list[i_id & MASK_IDNUM].tmat;
                case IDTYPE_NYID:
                    return this._idmk_list[i_id & MASK_IDNUM].tmat;
                case IDTYPE_PSID:
                    return this._psmk_list[i_id & MASK_IDNUM].tmat;
                default:
                    throw new NyARRuntimeException();
            }
        }

        public NyARIntPoint2d[] getVertex2D(int i_id)
        {
            switch (i_id & MASK_IDTYPE)
            {
                case IDTYPE_ARTK:
                    return this._armk_list[i_id & MASK_IDNUM].tl_vertex;
                case IDTYPE_NYID:
                    return this._idmk_list[i_id & MASK_IDNUM].tl_vertex;
                case IDTYPE_PSID:
                    return this._psmk_list[i_id & MASK_IDNUM].tl_vertex;
                default:
                    throw new NyARRuntimeException();
            }
        }

        public NyARIntPoint2d getCenter(int nMakerID)
        {
            switch (nMakerID & MASK_IDTYPE)
            {
                case IDTYPE_ARTK:
                    return this._armk_list[nMakerID & MASK_IDNUM].tl_center;
                case IDTYPE_NYID:
                    return this._idmk_list[nMakerID & MASK_IDNUM].tl_center;
                case IDTYPE_PSID:
                    return this._psmk_list[nMakerID & MASK_IDNUM].tl_center;
                default:
                    throw new NyARRuntimeException();
            }
        }

        public INyARRgbRaster getMarkerPlaneImage(int i_id, NyARSensor i_sensor, double i_x1, double i_y1, double i_x2,
            double i_y2, double i_x3, double i_y3, double i_x4, double i_y4, INyARRgbRaster i_raster)
        {
            return this.getPlaneImage(i_id, i_sensor, i_x1, i_y1, i_x2, i_y2, i_x3, i_y3, i_x4, i_y4, i_raster);
        }

        public INyARRgbRaster getMarkerPlaneImage(int i_id, NyARSensor i_sensor, double i_l, double i_t, double i_w,
            double i_h, INyARRgbRaster i_raster)
        {
            return this.getPlaneImage(i_id, i_sensor, i_l, i_t, i_w, i_h, i_raster);
        }

        public NyARDoubleMatrix44 getMarkerMatrix(int i_id)
        {
            return this.getTransformMatrix(i_id);
        }
        public NyARIntPoint2d[] getMarkerVertex2D(int i_id)
        {
            return this.getVertex2D(i_id);
        }
        public NyARDoublePoint3d getMarkerPlanePos(int i_id, int i_x, int i_y, NyARDoublePoint3d i_out)
        {
            return this.getPlanePos(i_id, i_x, i_y, i_out);
        }
        public bool isExistMarker(int i_id)
        {
            return this.isExist(i_id);
        }

      
        public void setBinThreshold(int i_th)
        {
            this._bin_threshold = i_th;
        }

        public void setConfidenceThreshold(double i_val)
        {
            this._armk_list.setConficenceTh(i_val);
        }

        public void setLostDelay(int i_delay)
        {
            this.lost_th = i_delay;
        }
        private long _time_stamp = -1;
        protected INyARHistogramAnalyzer_Threshold _hist_th;
        private OnSquareDetect _on_sq_handler;

        public int ThresholdValue = -1;

        public void update(NyARSensor i_sensor)
        {
            long time_stamp = i_sensor.getTimeStamp();
           
            if (this._time_stamp == time_stamp)
            {
                return;
            }
            int th = this._bin_threshold == THLESHOLD_AUTO
                ? this._hist_th.getThreshold(i_sensor.getGsHistogram())
                : this._bin_threshold;

            if (this.ThresholdValue != -1)
                th = this.ThresholdValue;

            //解析
            this._tracking_list.prepare();
            this._idmk_list.prepare();
            this._armk_list.prepare();
            this._psmk_list.prepare();
            //検出
            this._on_sq_handler.prepare(i_sensor.getPerspectiveCopy(), i_sensor.getGsImage(), th);
            this._sqdetect.detectMarkerCb(i_sensor, th, this._on_sq_handler);

            //検出結果の反映処理
            this._tracking_list.finish();
            this._armk_list.finish();
            this._idmk_list.finish();
            this._psmk_list.finish();

            for (int i = this._tracking_list.Count - 1; i >= 0; i--)
            {
                TMarkerData item = this._tracking_list[i];
                if (item.lost_count > this.lost_th)
                {
                    item.life = 0;
                }
                else if (item.sq != null)
                {
                    if (
                        !this._transmat.transMatContinue(item.sq, item.marker_offset, item.tmat,
                            item.last_param.last_error, item.tmat, item.last_param))
                    {
                        if (!this._transmat.transMat(item.sq, item.marker_offset, item.tmat, item.last_param))
                        {
                            item.life = 0; 
                        }
                    }
                }
            }
            for (int i = this._armk_list.Count - 1; i >= 0; i--)
            {
                TMarkerData target = this._armk_list[i];
                if (target.lost_count == 0)
                {
                    target.time_stamp = time_stamp;
                    if (target.life != 1)
                    {
                        continue;
                    }
                    this._transmat.transMat(target.sq, target.marker_offset, target.tmat, target.last_param);
                }
            }
            for (int i = this._idmk_list.Count - 1; i >= 0; i--)
            {
                TMarkerData target = this._idmk_list[i];
                if (target.lost_count == 0)
                {
                    target.time_stamp = time_stamp;
                    if (target.life != 1)
                    {
                        continue;
                    }
                    this._transmat.transMat(target.sq, target.marker_offset, target.tmat, target.last_param);
                }
            }
            for (int i = this._psmk_list.Count - 1; i >= 0; i--)
            {
                TMarkerData target = this._psmk_list[i];
                if (target.lost_count == 0)
                {
                    target.time_stamp = time_stamp;
                    if (target.life != 1)
                    {
                        continue;
                    }
                    this._transmat.transMat(target.sq, target.marker_offset, target.tmat, target.last_param);
                }
            }
            this._time_stamp = time_stamp;
            this._last_gs_th = th;
        }
    }

   
    class OnSquareDetect : NyARSquareContourDetector.CbHandler
    {
        private TrackingList _ref_tracking_list;
        private ARMarkerList _ref_armk_list;
        private NyIdList _ref_idmk_list;
        private ARPlayCardList _ref_psmk_list;

        public SquareStack _sq_stack;
        public INyARPerspectiveCopy _ref_input_rfb;
        public INyARGrayscaleRaster _ref_input_gs;
        public int _ref_th;


        private NyARCoord2Linear _coordline;
        public OnSquareDetect(
            NyARParam i_params,
            ARMarkerList i_armk_list, NyIdList i_idmk_list, ARPlayCardList i_psmk_list,
            TrackingList i_tracking_list, int i_initial_stack_size)
        {
            this._coordline = new NyARCoord2Linear(i_params.getScreenSize(), i_params.getDistortionFactor());
            this._ref_armk_list = i_armk_list;
            this._ref_idmk_list = i_idmk_list;
            this._ref_psmk_list = i_psmk_list;
            this._ref_tracking_list = i_tracking_list;
            this._sq_stack = new SquareStack(i_initial_stack_size);
        }
       
        public void setMaxDetectMarkerCapacity(int i_max_number_of_marker)
        {
            if (this._sq_stack.getArraySize() < i_max_number_of_marker)
            {
                this._sq_stack = new SquareStack(i_max_number_of_marker + 5);
            }
            return;
        }
       
        public void prepare(INyARPerspectiveCopy i_pcopy, INyARGrayscaleRaster i_gs, int th)
        {
            this._ref_input_rfb = i_pcopy;
            this._ref_input_gs = i_gs;
            this._ref_th = th;
            this._sq_stack.clear();
        }
        public void detectMarkerCallback(NyARIntCoordinates i_coord, int[] i_vertex_index)
        {
            //とりあえずSquareスタックを予約
            SquareStack.Item sq_tmp = this._sq_stack.prePush();
            //確保できない(1つのdetectorが複数の候補を得る場合(同じARマーカが多くある場合など)に発生することがある。)
            if (sq_tmp == null)
            {
                return;
            }

            //観測座標点の記録
            for (int i2 = 0; i2 < 4; i2++)
            {
                sq_tmp.ob_vertex[i2].setValue(i_coord.items[i_vertex_index[i2]]);
            }
            //頂点分布を計算
            sq_tmp.vertex_area.setAreaRect(sq_tmp.ob_vertex, 4);
            //頂点座標の中心を計算
            sq_tmp.center2d.setCenterPos(sq_tmp.ob_vertex, 4);
            //矩形面積
            sq_tmp.rect_area = sq_tmp.vertex_area.w * sq_tmp.vertex_area.h;

            bool is_target_marker = false;
            for (;;)
            {
                //トラッキング対象か確認する。
                if (this._ref_tracking_list.update(sq_tmp))
                {
                    //トラッキング対象ならブレーク
                    is_target_marker = true;
                    break;
                }
                //@todo 複数マーカ時に、トラッキング済のarmarkerを探索対象外に出来ない？

                //nyIdマーカの特定(IDマーカの特定はここで完結する。)
                if (this._ref_idmk_list.Count > 0)
                {
                    if (this._ref_idmk_list.update(this._ref_input_gs, sq_tmp))
                    {
                        is_target_marker = true;
                        break;//idマーカを特定
                    }
                }
                //PSARマーカの特定(IDマーカの特定はここで完結する。)
                if (this._ref_psmk_list.Count > 0)
                {
                    if (this._ref_psmk_list.update(this._ref_input_gs, sq_tmp))
                    {
                        is_target_marker = true;
                        break;//idマーカを特定
                    }
                }
                //ARマーカの特定
                if (this._ref_armk_list.Count > 0)
                {
                    //敷居値により1個のマーカに対して複数の候補が見つかることもある。
                    if (this._ref_armk_list.update(this._ref_input_rfb, sq_tmp))
                    {
                        is_target_marker = true;
                        break;
                    }
                }
                break;
            }
            //この矩形が検出対象なら、矩形情報を精密に再計算
            if (is_target_marker)
            {
                //矩形は検出対象にマークされている。
                for (int i2 = 0; i2 < 4; i2++)
                {
                    this._coordline.coord2Line(i_vertex_index[i2], i_vertex_index[(i2 + 1) % 4], i_coord, sq_tmp.line[i2]);
                }
                for (int i2 = 0; i2 < 4; i2++)
                {
                    //直線同士の交点計算
                    if (!sq_tmp.line[i2].crossPos(sq_tmp.line[(i2 + 3) % 4], sq_tmp.sqvertex[i2]))
                    {
                        throw new NyARRuntimeException();//まずない。ありえない。
                    }
                }
            }
            else
            {
                //この矩形は検出対象にマークされなかったので、解除
                this._sq_stack.pop();
            }
        }
    }


    class SquareDetect : INyARMarkerSystemSquareDetect
    {
        private NyARSquareContourDetector_Rle _sd;
        public SquareDetect(INyARMarkerSystemConfig i_config)
        {
            this._sd = new NyARSquareContourDetector_Rle(i_config.getScreenSize());
        }
        public void detectMarkerCb(NyARSensor i_sensor, int i_th, NyARSquareContourDetector.CbHandler i_handler)
        {
            this._sd.detectMarker(i_sensor.getGsImage(), i_th, i_handler);
        }
    }

}
