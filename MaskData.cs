using System;
using System.Collections.Generic;


namespace YOLIC
{
    /// <summary>
    /// A structure for storing masks and their related data in batched format.
    /// Implements basic filtering and concatenation.
    /// </summary>
    class MaskData
    {
        public int[] mShape;
        public List<float> mMask;
        public List<float> mIoU;
        public List<float> mStalibility;
        public List<int> mBox;

        public List<List<float>> mfinalMask;
        public MaskData()
        {
            this.mShape = new int[4];
            this.mMask = new List<float>();
            this.mIoU = new List<float>();
            this.mStalibility = new List<float>();
            this.mBox = new List<int>();

            this.mfinalMask = new List<List<float>>();
        }

    }
}
