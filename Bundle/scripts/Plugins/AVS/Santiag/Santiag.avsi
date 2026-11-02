# santiag v1.6
# Simple antialiasing

# This program is free software. It comes without any warranty, to
# the extent permitted by applicable law. You can redistribute it
# and/or modify it under the terms of the Do What The Fuck You Want
# To Public License, Version 2, as published by Sam Hocevar. See
# http://sam.zoy.org/wtfpl/COPYING for more details.

# type = "nnedi3", "eedi2", "eedi3" or "sangnom"
# "eedi3" requires v0.9.2 or later
# "sangnom" requires sangnom2
Function santiag (clip c, int "strh", int "strv", string "type", int "nns", int "aa", int "aac", int "threads", int "nsize", int "vcheck", int "fw", int "fh", bool "halfres", string "scaler_post", int "maskt", string "typeh", string "typev")
{
	strh        = Default (strh,                       1)
	strv        = Default (strv,                       1)
	type        = Default (type,                "nnedi3")
	typeh       = Default (typeh,                   type)
	typev       = Default (typev,                   type)
	halfres     = Default (halfres,                false)
	scaler_post = Default (scaler_post, "Spline36Resize")

	c

	w   = Width ()
	h   = Height ()
	fwh = (strv < 0) ? fw : w
	fhh = (strv < 0) ? fh : h

	(strh >= 0) ?             santiag_dir (strh, typeh, halfres, scaler_post, nns=nns, aa=aa, aac=aac, threads=threads, nsize=nsize, vcheck=vcheck, maskt=maskt, fw=fwh, fh=fhh)              : last
	(strv >= 0) ? TurnLeft ().santiag_dir (strv, typev, halfres, scaler_post, nns=nns, aa=aa, aac=aac, threads=threads, nsize=nsize, vcheck=vcheck, maskt=maskt, fw=fh,  fh=fw ).TurnRight () : last

	fwx = Default (fw, w)
	fhx = Default (fh, h)
	(strh < 0 && strv < 0) ? Eval (scaler_post + " (fwx, fhx)") : last
}

Function santiag_dir (clip c, int strength, string type, bool halfres, string scaler_post, int "nns", int "aa", int "aac", int "threads", int "nsize", int "vcheck", int "maskt", int "fw", int "fh")
{
	c
	w = Width ()
	h = Height ()
	fw = Default (fw, w)
	fh = Default (fh, h)

	multi = (IsYUV () && ! IsYV12 ())
	c_y = (multi             ) ?         ConvertToYV12 () : last
	c_u = (multi && ! IsY8 ()) ? UToY ().ConvertToYV12 () : last
	c_v = (multi && ! IsY8 ()) ? VToY ().ConvertToYV12 () : last
	c_y =                        c_y.santiag_stronger (strength, type, halfres, multi, nns=nns, aa=aa, aac=aac, threads=threads, nsize=nsize, vcheck=vcheck, maskt=maskt)
	c_u = (multi && ! IsY8 ()) ? c_u.santiag_stronger (strength, type, halfres, true , nns=nns, aa=aa, aac=aac, threads=threads, nsize=nsize, vcheck=vcheck, maskt=maskt) : c_u
	c_v = (multi && ! IsY8 ()) ? c_v.santiag_stronger (strength, type, halfres, true , nns=nns, aa=aa, aac=aac, threads=threads, nsize=nsize, vcheck=vcheck, maskt=maskt) : c_v
	  (multi && ! IsY8 ()) ? YToUV (c_u, c_v, c_y)
\	: IsY8 ()              ? c_y.ConvertToY8 ()
\	:                        c_y

	Eval (scaler_post + " (fw, fh, src_left=0, src_top=(halfres)?0:0.5, src_width=Width (), src_height=Height ())")
}

Function santiag_stronger (clip c, int strength, string type, bool halfres, bool lumaonly, int "nns", int "aa", int "aac", int "threads", int "nsize", int "vcheck", int "maskt")
{
	strength = (strength < 0) ? 0 : strength
	field = strength % 2
	dh = (strength <= 0 && ! halfres)

	(strength > 0) ? c.santiag_stronger (strength - 1, type, halfres, lumaonly, nns=nns, aa=aa, aac=aac, threads=threads, nsize=nsize, vcheck=vcheck, maskt=maskt) : c

	  (type == "nnedi3" ) ? santiag_nnedi3  (dh, field, lumaonly, nns=nns, threads=threads, nsize=nsize)
\	: (type == "eedi2"  ) ? santiag_eedi2   (dh, field)
\	: (type == "eedi3"  ) ? santiag_eedi3   (dh, field, lumaonly, nns=nns, threads=threads, nsize=nsize, vcheck=vcheck, maskt=maskt)
\	: (type == "sangnom") ? santiag_sangnom (dh, field, aa=aa, aac=aac)
\	: Assert (false, "Santiag: unexpected value for type.")
}

Function santiag_nnedi3 (clip c, bool dh, int field, bool lumaonly, int "nns", int "threads", int "nsize")
{
	c.nnedi3 (dh=dh, field=field, nns=nns, threads=threads, nsize=nsize, u=!lumaonly, v=!lumaonly)
}

Function santiag_eedi2 (clip c, bool dh, int field)
{
	c
	w = Width ()
	h = Height ()
	(dh) ? last : PointResize (w, h / 2, 0, 1-field, w, h)
	eedi2 (field=field)
}

# maskt = 24
Function santiag_eedi3 (clip c, bool dh, int field, bool lumaonly, int "nns", int "threads", int "nsize", int "vcheck", int "maskt")
{
	chroma = (lumaonly) ? 1 : 3
	sclip = c.santiag_nnedi3 (dh, field, lumaonly, nns=nns, threads=threads, nsize=nsize)
	mclip_flag = (Defined (maskt) && maskt > 0)

	mclip = (mclip_flag && dh) ? sclip.BicubicResize (c.Width (), c.Height (), src_top=0.5-field) : sclip
	mclip = (mclip_flag)
\	?	mclip.mt_edge (mode="1 2 1 0 0 0 -1 -2 -1 1", thY1=0, thY2=255, u=chroma, v=chroma)
\			.mt_binarize (threshold=maskt, y=3, u=chroma, v=chroma)
\			.mt_expand (y=3, u=chroma, v=chroma)
\	:	santiag_undef ()

	c.eedi3 (dh=dh, field=field, threads=threads, vcheck=vcheck, sclip=sclip, mclip=mclip, u=!lumaonly, v=!lumaonly)
}

Function santiag_sangnom (clip c, bool dh, int field, int "aa", int "aac")
{
	c
	w = Width ()
	h = Height ()
	(dh) ? Spline36Resize (w, h * 2, 0, -0.25, w, h) : last
	SangNom2 (order=field, aa=aa, aac=aac)
}

Function santiag_undef () { }