using System;
using Microsoft.DotNet.InternalAbstractions;
using Microsoft.Extensions.DependencyModel;
using AutoAdapter;
using AutoAdapter.Extensions;

namespace AdapterTestApp
{
    public class AdapterTestApp
    {

        static void Main(string[] args)
        {
            //Test1();
            Test2();
        }

        private static void Test1()
        {
            Console.WriteLine("Hello World!");

            Test test = new Test()
            {
                Name = "Hello"
            };

            ITest adaptedTest = test.CreateAdapter<ITest>();

            Console.WriteLine("ITest.Name: {0}", adaptedTest.Name);

            IAdaptedObject adaptedObj = adaptedTest as IAdaptedObject;
            if (adaptedObj != null)
            {
                Console.WriteLine("Object is adapted: {0}", adaptedObj.AdaptedObject.ToString());

                ((Test)adaptedObj.AdaptedObject).Name = "Changed";
            }

            Console.WriteLine("ITest.Name: {0}", adaptedTest.Name);

            Console.WriteLine("------------------------");

            Type beginFuncType;
            Type endFuncType;
            Type taskFactoryType;
            var ext = new APMToTaskAdapterExtension();
            var method = ext.MakeTaskFactoryMethodAndBeginEndFuncTypes(typeof(string), out beginFuncType, out endFuncType, out taskFactoryType, typeof(int), typeof(string));

            Console.WriteLine("Method: {0}", method);
            Console.WriteLine("------------------------");
            Console.WriteLine("Begin: {0}", beginFuncType);
            Console.WriteLine("------------------------");
            Console.WriteLine("End: {0}", endFuncType);
            Console.WriteLine("------------------------");
            Console.WriteLine("TaskFactory: {0}", taskFactoryType);
            Console.WriteLine("------------------------");
        }


        private static void Test2()
        {
            AdaptedTestObject obj = new AdaptedTestObject()
            {
                Name = "Hello",
                Address = "World",
                Child = new AdaptedChildObject()
                {
                    Test = "yikes"
                }
            };

            Console.WriteLine("Original object:");
            Console.WriteLine();
            Console.WriteLine("obj.Name    = {0}", obj.Name);
            Console.WriteLine("obj.Address = {0}", obj.Address);
            Console.WriteLine("obj.Child   = {0}", obj.Child.Test);
            Console.WriteLine();

            Console.WriteLine("IAdapted1:");
            Console.WriteLine();
            IAdapted1 adaptedtest = obj.CreateAdapter<IAdapted1>();
            Console.WriteLine("adapted1.GetType().FullName = {0}", adaptedtest.GetType().FullName);
            Console.WriteLine("adapted1.Child.GetType().FullName = {0}", adaptedtest.GetChild().GetType().FullName);

            Console.WriteLine("adapted1.Name: {0}", adaptedtest.Name);
            Console.WriteLine("adapted1.Child.Test: {0}", adaptedtest.GetChild().Test);

        }
    }
}
