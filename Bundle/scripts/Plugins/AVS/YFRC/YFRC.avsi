# Author: RunForLife(Oleg Yushko) http://videomontazh.com.ua
# http://avisynth.nl/index.php/YFRC
#
# This program is free software; you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation; either version 2 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program; if not, write to the Free Software
# Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA, or visit
# http:#www.gnu.org/copyleft/gpl.html.

#LoadPlugin("C:\Program Files\AviSynth 2.5\plugins\MvTools2\mvtools2.dll")
#LoadPlugin("C:\Program Files\AviSynth 2.5\plugins\MaskTools2.0a48\mt_masktools-26.dll")

# If you feel bad with blending, you can turn it off with: YFRC(OverlayType=1)

######################################################################################################################################################################################################################
function YFRC(clip clp_Input, int "BlockH", int "BlockV", int "OverlayType", int "MaskExpand")
{
#Yushko Frame Rate Converter 2x
#ColorSpace: YV12
#OverlayType: 0 - soft (blend), 1 - hard (SelectOdd)

clp_input      = ConvertToYV12(clp_input)                      # script produce YV12 colorspace (3 times faster than YUY2!!!)
clp_Super      = MSuper(clp_Input.blur(1), chroma=true, pel=2) # blur(1) - smooth edges for better analysing
ox             = clp_Input.width()
oy             = clp_Input.height()
fps_num        = FrameRateNumerator(clp_input)*2  # Numerator  , always 2X
fps_den        = FrameRateDenominator(clp_input)  # Denominator, always same as input clip

fps_num        = ((fps_num==0||fps_den==0)) ? FramerateNumerator(clp_input)*2 : fps_num
fps_den        = ((fps_num==0||fps_den==0)) ? FramerateDenominator(clp_input) : fps_den
MaskExpand     = default(MaskExpand, 1) # 1 or 2
OverlayType    = default(OverlayType, 0) # 0 - like ConvertFPS (blend); 1 - like ChangeFPS (strong);
blendSOFT      = (OverlayType==0) ? clp_Input.ConvertFPS(fps_num, fps_den).SelectOdd() : DeleteFrame(clp_Input, 0)
BlockH         = default(BlockH, 16) # use 8 for 320x240 (WEB); 16 for 720x576 (SD); 32 for 1280x720 (720p HD); 32 for 1920x1080 (1080p HD)
BlockV         = default(BlockV, 16) # use 8 for 320x240 (WEB); 16 for 720x576 (SD); 32 for 1280x720 (720p HD); 32 for 1920x1080 (1080p HD)
blendHARD      = DeleteFrame(clp_Input, 0) # SceneChange detection

bw1_vec116     = clp_Super.MAnalyse(blksize=BlockH, blksizeV=BlockV, isb=true , chroma=false, search=1, searchparam=1, truemotion=true, lambda=2000, global=true, dct=0, divide=2)
fw1_vec116     = clp_Super.MAnalyse(blksize=BlockH, blksizeV=BlockV, isb=false, chroma=false, search=1, searchparam=1, truemotion=true, lambda=2000, global=true, dct=0, divide=2)
ErrorMask16L   = MMask(clp_input, bw1_vec116, kind=1)
ErrorMask16R   = DeleteFrame(MMask(clp_input, fw1_vec116, kind=1), 0)
ErrorMask16    = Overlay(ErrorMask16L, ErrorMask16R, opacity=0.5, mode="Lighten")#.ColorYUV(gain_y=256)
SceneChange    = MSCDetection(clp_input, bw1_vec116, thSCD2=130)
FPSconverted16 = clp_input.MFlowFps(clp_input.MSuper(levels=1), bw1_vec116, fw1_vec116, num=fps_num, den=fps_den, blend=false, mask=0) #mask=0 - doesn't matter what mode is

CircleExpand   = mt_circle(radius=MaskExpand, zero=true)
CircleInpand   = mt_circle(radius=1         , zero=true)
ErrorMask16    = ErrorMask16.BicubicResize(Round((Ox/BlockH)/4)*4, Round((Oy/BlockV)/4)*4).mt_expand(mode=CircleExpand).mt_inpand(mode=CircleInpand).mt_binarize(64).Blur(1).BicubicResize(ox, oy)

ClipToReturn   = mt_merge(SelectOdd(FPSconverted16), blendSOFT, ErrorMask16, luma=true)
ClipToReturn   = mt_merge(ClipToReturn, blendHARD, SceneChange, luma=true)
ClipToReturn   = Interleave(clp_Input, ClipToReturn)

return ClipToReturn
}
######################################################################################################################################################################################################################