using System;
using AutoProperty;
namespace SandBox
{
    public partial class Class1
    {
        [AutoProp(AXS.PublicGetPrivateSet)]private int _myInt;

        [AutoProp(typeof(int),AXS.PublicGetSet)]private float _myFloat, _myFloat2;

        public void Test()
        {
            MyFloat = 2;
        }
    }
}