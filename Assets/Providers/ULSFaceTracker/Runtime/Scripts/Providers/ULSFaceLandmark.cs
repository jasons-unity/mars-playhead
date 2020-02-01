namespace Unity.Labs.MARS
{
    // from ULS landmarks to authoring landmarks
    // 2 == chin
    // 9 == nose bridge
    // 10 = nose tip
    // 5,6 averaged = right eyebrow
    // 7,8 averaged = left eyebrow
    // 23, 24, 28 averaged == upper lip
    // 26, 27, 29 averaged == lower lip
    // 28, 29, right lip corner,left lip corner averaged == mouth
    // 14-17 averaged = right eye
    // 18-21 averaged = left eye

    public enum ULSFaceLandmarks
    {
        RightJaw0,
        RightJaw1,
        RightJaw2,
        RightJaw3,
        RightJaw4,
        RightJaw5,
        RightJaw6,
        ChinRight7,
        ChinMiddle8,
        ChinLeft9,
        LeftJaw10,
        LeftJaw11,
        LeftJaw12,
        LeftJaw13,
        LeftJaw14,
        LeftJaw15,
        LeftJaw16,
        RightEyebrowOuter17,
        RightEyebrow18,
        RightEyebrow19,
        RightEyebrow20,
        RightEyebrowInner21,
        LeftEyebrowInner22,
        LeftEyebrow23,
        LeftEyebrow24,
        LeftEyebrow25,
        LeftEyebrowOuter26,
        NoseBridge27,
        NoseBridge28,
        NoseBridge29,
        NoseTip30,
        RightNostril31,
        RightNostril32,
        Septum33,
        LeftNostril34,
        LeftNostril35,
        RightEyeOuter36,
        RightEyeUpper37,
        RightEyeUpper38,
        RightEyeInner39,
        RightEyeLower40,
        RightEyeLower41,
        LeftEyeInner42,
        LeftEyeUpper43,
        LeftEyeUpper44,
        LeftEyeOuter45,
        LeftEyeLower46,
        LeftEyeLower47,
        RightLipCorner48,
        UpperLipRight49,
        UpperLipRight50,
        UpperLipMiddle51,
        UpperLipLeft52,
        UpperLipLeft53,
        LeftLipCorner54,
        LowerLipLeft55,
        LowerLipLeft56,
        LowerLipMiddle57,
        LowerLipRight58,
        LowerLipRight59,
        UpperLipBottomRight60,
        UpperLipBottomMiddle61,
        UpperLipBottomLeft62,
        LowerLipTopLeft63,
        LowerLipTopMiddle64,
        LowerLipTopRight65,
    }
}
