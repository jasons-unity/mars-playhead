#ifndef XRAY_COMMON_INCLUDED
#define XRAY_COMMON_INCLUDED


static const half _FloorEdgeFadeScale = 10;
static const half _CeilingPeelbackScale = 5;
static const half _FadeThickness = 0.025;

half3 _RoomCenter = half3(0,0,0);
half _FloorHeight = 0.0;
half _CeilingHeight = 2.5;
half _RoomClipOffset = .5;
half _XRayScale = 1.0;

half getXRayFade(half3 coords)
{
    coords = (coords - _RoomCenter) / _XRayScale;
    half cameraY = (_WorldSpaceCameraPos.y - _RoomCenter.y) / _XRayScale;

    // Get where the clip plane should cut off at, further adjusted by camera height
    half clipPlaneThickness = _RoomClipOffset - _CeilingPeelbackScale*max(cameraY - _CeilingHeight,0)*saturate(coords.y - _CeilingHeight);

    // Get the current pixel's distance from the clip plane
    half2 clipLine = -normalize(half2(unity_CameraToWorld._m02,unity_CameraToWorld._m22));
    half2x2 toClipSpace = half2x2(half2(clipLine.x, clipLine.y), half2(-clipLine.y, clipLine.x));
    half clipDistance = mul(toClipSpace, coords.xz).x;

    half inFrontOfClipPlane = saturate(coords.y - _FloorHeight)*saturate(clipDistance - clipPlaneThickness);
    half cameraAndPixelBelowFloorPlane = saturate(_FloorHeight - _WorldSpaceCameraPos.y)*saturate(_FloorHeight - coords.y);

    // Stop drawing if we are outside any of the drawing regions
    clip(-1 * (inFrontOfClipPlane + cameraAndPixelBelowFloorPlane)
        #ifdef SHADOWS_DEPTH            // Hack to detect we are in the shadow-caster phase (but not shadow receiver) and properly prevent the light from clipping out
            + UNITY_MATRIX_P[3][3]*4
        #endif
     );

    return  1 - saturate((clipDistance + _FadeThickness - clipPlaneThickness) / _FadeThickness)*saturate((coords.y - _FloorHeight)*_FloorEdgeFadeScale);
}

half getXRayEdgeFade(half3 coords)
{
    coords = (coords - _RoomCenter) / _XRayScale;
    half cameraY = (_WorldSpaceCameraPos.y - _RoomCenter.y) / _XRayScale;

    // Get where the clip plane should cut off at, further adjusted by camera height
    half clipPlaneThickness = _RoomClipOffset - _CeilingPeelbackScale*max(cameraY - _CeilingHeight,0)*saturate(coords.y - _CeilingHeight);

    // Get the current pixel's distance from the clip plane
    half2 clipLine = -normalize(half2(unity_CameraToWorld._m02,unity_CameraToWorld._m22));
    half2x2 toClipSpace = half2x2(half2(clipLine.x, clipLine.y), half2(-clipLine.y, clipLine.x));
    half clipDistance = mul(toClipSpace, coords.xz).x;

    half betweenFadePlanes = saturate(coords.y - _FloorHeight)*(saturate(clipDistance - _FadeThickness - clipPlaneThickness) + saturate(-clipDistance + clipPlaneThickness));
    half pixelBelowFloorPlane = saturate(_FloorHeight - coords.y);

    // Stop drawing if we are outside any of the drawing regions
    clip(-1 * (betweenFadePlanes + pixelBelowFloorPlane));

    return 1 - saturate((clipDistance - clipPlaneThickness)/ _FadeThickness)*saturate((coords.y - _FloorHeight)*_FloorEdgeFadeScale);
}

#endif
