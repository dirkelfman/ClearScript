// 
// Copyright (c) Microsoft Corporation. All rights reserved.
// 
// Microsoft Public License (MS-PL)
// 
// This license governs use of the accompanying software. If you use the
// software, you accept this license. If you do not accept the license, do not
// use the software.
// 
// 1. Definitions
// 
//   The terms "reproduce," "reproduction," "derivative works," and
//   "distribution" have the same meaning here as under U.S. copyright law. A
//   "contribution" is the original software, or any additions or changes to
//   the software. A "contributor" is any person that distributes its
//   contribution under this license. "Licensed patents" are a contributor's
//   patent claims that read directly on its contribution.
// 
// 2. Grant of Rights
// 
//   (A) Copyright Grant- Subject to the terms of this license, including the
//       license conditions and limitations in section 3, each contributor
//       grants you a non-exclusive, worldwide, royalty-free copyright license
//       to reproduce its contribution, prepare derivative works of its
//       contribution, and distribute its contribution or any derivative works
//       that you create.
// 
//   (B) Patent Grant- Subject to the terms of this license, including the
//       license conditions and limitations in section 3, each contributor
//       grants you a non-exclusive, worldwide, royalty-free license under its
//       licensed patents to make, have made, use, sell, offer for sale,
//       import, and/or otherwise dispose of its contribution in the software
//       or derivative works of the contribution in the software.
// 
// 3. Conditions and Limitations
// 
//   (A) No Trademark License- This license does not grant you rights to use
//       any contributors' name, logo, or trademarks.
// 
//   (B) If you bring a patent claim against any contributor over patents that
//       you claim are infringed by the software, your patent license from such
//       contributor to the software ends automatically.
// 
//   (C) If you distribute any portion of the software, you must retain all
//       copyright, patent, trademark, and attribution notices that are present
//       in the software.
// 
//   (D) If you distribute any portion of the software in source code form, you
//       may do so only under this license by including a complete copy of this
//       license with your distribution. If you distribute any portion of the
//       software in compiled or object code form, you may only do so under a
//       license that complies with this license.
// 
//   (E) The software is licensed "as-is." You bear the risk of using it. The
//       contributors give no express warranties, guarantees or conditions. You
//       may have additional consumer rights under your local laws which this
//       license cannot change. To the extent permitted under your local laws,
//       the contributors exclude the implied warranties of merchantability,
//       fitness for a particular purpose and non-infringement.
//       

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Threading;
using System.Windows.Threading;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.ClearScript.Util;
using Microsoft.ClearScript.V8;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Microsoft.ClearScript.Test
{

    public static class DateExt1
    {
        public static int getMonth(this DateTime dt)
        {
            return dt.Month;
        }
    }
    public static class V8PollyFillListExtensions
    {
        public static int push<T>(this IList<T> list, object item)
        {
            if (item == null)
            {
                list.Add(default(T));
            }
            else
            {
                if (typeof(T).IsAssignableFrom(item.GetType()))
                {
                    list.Add((T)item);
                }
                else if (list is JArray)
                {
                    ((JArray)list).Add(JToken.FromObject(item));
                }
                else
                {
                    var converted = Convert.ChangeType(item, typeof(T));
                    list.Add((T)converted);
                }
            }


            return list.Count;
        }

        public static object Unwrap(this object obj)
        {
            if (obj is JValue)
            {
                return ((JValue)obj).Value;
            }
            return obj;
        }
        public static object pop(this IList list)
        {


            if (list == null || list.Count == 0)
            {
                //todo return undefined.
                return Undefined;
            }
            else
            {
                var ret = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
                return ret.Unwrap();
            }

        }

        public static object Undefined
        {
            get { return null; }//t.Undefined.Value; }
        }


        public static object shift(this IList list)
        {
            if (list == null || list.Count == 0)
            {
                //todo return undefined.
                return Undefined;
            }
            else
            {
                var ret = list[0];
                list.RemoveAt(0);
                return ret.Unwrap();
            }

        }



    }
    [TestClass]
    [DeploymentItem("ClearScriptV8-64.dll")]
    [DeploymentItem("ClearScriptV8-32.dll")]
    [DeploymentItem("v8-x64.dll")]
    [DeploymentItem("v8-ia32.dll")]
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Test classes use TestCleanupAttribute for deterministic teardown.")]
    public class V8ScriptEngineTest : ClearScriptTest
    {
        #region setup / teardown

        private V8ScriptEngine engine;

        [TestInitialize]
        public void TestInitialize()
        {
            engine = new V8ScriptEngine(V8ScriptEngineFlags.EnableDebugging, 5858);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            engine.Dispose();
            BaseTestCleanup();
        }

        #endregion

        #region test methods

        // ReSharper disable InconsistentNaming

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddHostObject()
        {
            var host = new HostFunctions();
            engine.AddHostObject("host", host);
            Assert.AreSame(host, engine.Evaluate("host"));
        }


        [TestMethod, TestCategory("V8ScriptEngine")]
        public void ByteArrToString()
        {
            

        
          
            var arrayBuffer = (Microsoft.ClearScript.V8.IV8ArrayBuffer)engine.Evaluate(" var buffer = new ArrayBuffer(16); var uarr =new Uint8Array(buffer); uarr[2]=16; uarr[1]=5; x= buffer;");



            var c = arrayBuffer.ByteLength;
            byte[] buffer = new byte[16];
            arrayBuffer.ReadBuffer(buffer, 0,0, 16);
            Assert.AreEqual(buffer[2], Convert.ToByte(16));
            Assert.AreEqual(buffer[1], Convert.ToByte(5));

            byte[] inbytes = System.Text.Encoding.UTF8.GetBytes("hello");
            arrayBuffer.WriteBuffer(inbytes, 0, 0, inbytes.Length);


            arrayBuffer.ReadBuffer(buffer, 0,0, 16);
            var hello=System.Text.Encoding.UTF8.GetString(buffer,0, inbytes.Length);
            Assert.AreEqual("hello", hello);


            Assert.AreEqual(arrayBuffer.GetJsType(), JsTypes.jsArrayBuffer);




        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void ReadLargeBuffer()
        {




            var arrayBuffer = (Microsoft.ClearScript.V8.IV8ArrayBuffer)engine.Evaluate(" var buffer = new ArrayBuffer(5000); var uarr =new Uint8Array(buffer); for( var i =0;i<uarr.length;i++){ uarr[i]=i}; x= buffer;");
            var len = arrayBuffer.ByteLength;

            byte[] buffer = new byte[6];
            MemoryStream ms = new MemoryStream();
            var pos = 0;
            while (len - pos > 0)
            {
                var readLn = Math.Min(len - pos, buffer.Length);

                arrayBuffer.ReadBuffer(buffer,  pos,0, readLn);
                ms.Write(buffer, 0, readLn);
                pos += readLn;
            }
            var outArr = ms.ToArray();
            for (var i = 0; i < outArr.Length; i++)
            {
              
                Assert.AreEqual(i%256, (int)outArr[i], i.ToString());
            }






        }




        public class PointerConverter
        {
            private static readonly ConstructorInfo _intPtrCtor = typeof(IntPtr).GetConstructor(
                new Type[] { Type.GetType("System.Void*") });

            private static readonly MethodInfo _toPointer = typeof(IntPtr).GetMethod(
                    "ToPointer",
                    BindingFlags.Instance | BindingFlags.Public);

            private static readonly object[] _emptyArray = new object[0];

            public static Pointer IntPtrToPointer(IntPtr intPtr)
            {
                return (Pointer)_toPointer.Invoke(intPtr, _emptyArray);
            }

            public static IntPtr PointerToIntPtr(Pointer ptr)
            {
                return (IntPtr)_intPtrCtor.Invoke(new object[1] { ptr });
            }
        }






        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_ThrowErrorWithStuff()
        {
            object error = engine.Evaluate("var error = new Error('hey');error.prop1='zeebra';error;");

            try
            {
                engine.Execute("throw error;");
            }
            catch (Microsoft.ClearScript.ScriptEngineException ex)
            {
               
                var prop1Val = JObject.Parse(ex.ErrorJson).GetValue("prop1").Value<string>();
                Assert.AreEqual("zeebra", prop1Val);
              
            }
           
  
            try
            {
                engine.Execute("function MyError(message) { this.name = 'MyError';  this.fart = 'Default Message';}MyError.prototype = Object.create(Error.prototype);MyError.prototype.constructor = MyError;throw new MyError('steve');");
            }
            catch (Microsoft.ClearScript.ScriptEngineException ex)
            {
               
                var jtoken = JToken.FromObject(ex.ErrorJson);
                Assert.IsNotNull(jtoken);
                Assert.AreEqual(ex != null, true);
          
            }

           
            try
            {
                Action a = () => { throw new NotFiniteNumberException("bing", 123); };
                engine.Execute("function Doit(fn) { fn();}");
                engine.Script.Doit(a);
            }
            catch (Microsoft.ClearScript.ScriptEngineException ex)
            {

                
                Assert.IsNull(ex.ErrorJson);
         

            }


        }


      
        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Jtokens1()
        {

            var jobj = JObject.Parse("{ a: 123 , a1:'food' , b:{ c:1 , d:[1,2,3], e:[{f:1},{f:2}]}}");
            engine.AddHostObject("jobj", jobj);
            var jobjV = engine.Evaluate("jobj.a");
            Assert.AreEqual(jobjV, (object)123);
            Assert.AreEqual(engine.Evaluate("jobj.b.c"),1);
            Assert.AreEqual(engine.Evaluate("jobj.b.d[2]"), 3);

            Assert.AreEqual("aa1b", engine.Evaluate("var ret='';for( k in jobj){ret+=k;};ret=ret;"));
            

           
            engine.Evaluate("jobj.a =234");
            Assert.AreEqual(engine.Evaluate("jobj.a"), (object)234);

            engine.Evaluate("jobj.a2 ={'a2a':1234};");
            Assert.AreEqual(engine.Evaluate("jobj.a2.a2a"), (object)1234);

            engine.Evaluate("jobj.a3 =[8,9,10];");
            Assert.AreEqual(engine.Evaluate("jobj.a3[1]"), 9);


            engine.Evaluate("jobj.a3[1] =25;");
            Assert.AreEqual(engine.Evaluate("jobj.a3[1]"),25);

            engine.Evaluate("jobj.a3.push('a');");
            Assert.AreEqual(engine.Evaluate("jobj.a3[3]"), "a");

        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Jtokens_extensionMethods()
        {
            engine.AddHostType("dateExt1",typeof(DateExt1));
            engine.AddHostType("v8poly", typeof(V8PollyFillListExtensions));
            var fn = (dynamic)engine.Evaluate("x=function(x){ return x.getMonth();};");
            var month = (int)fn(DateTime.Now);

            Assert.AreEqual(DateTime.Now.Month, month);

            JObject job = new JObject();
            job["date"] = JToken.FromObject(DateTime.Now);
            fn = (dynamic)engine.Evaluate("x=function(x){ return x.date.getMonth();};");
            month = fn(job);
            Assert.AreEqual(DateTime.Now.Month, month);

            var jarry = JArray.Parse("[1,2,3]");
            var regArra = new List<int> { 1, 2, 3 };
            var j = V8PollyFillListExtensions.pop(jarry);
            var pop = (dynamic)engine.Evaluate("x=function(x){ return x.pop();};");


            var reqArrayPop = pop(regArra);
            var val = pop(jarry);


        }






        [TestMethod, TestCategory("V8ScriptEngine")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void V8ScriptEngine_AddHostObject_Scalar()
        {
            engine.AddHostObject("value", 123);
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddHostObject_Enum()
        {
            const DayOfWeek value = DayOfWeek.Wednesday;
            engine.AddHostObject("value", value);
            Assert.AreEqual(value, engine.Evaluate("value"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddHostObject_Struct()
        {
            var date = new DateTime(2007, 5, 22, 6, 15, 43);
            engine.AddHostObject("date", date);
            Assert.AreEqual(date, engine.Evaluate("date"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddHostObject_GlobalMembers()
        {
            var host = new HostFunctions();
            engine.AddHostObject("host", HostItemFlags.GlobalMembers, host);
            Assert.IsInstanceOfType(engine.Evaluate("newObj()"), typeof(PropertyBag));

            engine.AddHostObject("test", HostItemFlags.GlobalMembers, this);
            engine.Execute("TestProperty = newObj()");
            Assert.IsInstanceOfType(TestProperty, typeof(PropertyBag));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddHostObject_GlobalMembers_Overwrite()
        {
            const int fooFirst = 123;
            const int fooSecond = 456;
            const int barSecond = 789;
            engine.AddHostObject("bar", HostItemFlags.GlobalMembers, new { second = barSecond });
            engine.AddHostObject("foo", HostItemFlags.GlobalMembers, new { second = fooSecond });
            engine.AddHostObject("foo", HostItemFlags.GlobalMembers, new { first = fooFirst });
            Assert.AreEqual(fooFirst, engine.Evaluate("first"));
            Assert.AreEqual(barSecond, engine.Evaluate("second"));
        }


        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_CanWriteHeapSnapshot()
        {
            const int fooFirst = 123;
            const int fooSecond = 456;
            const int barSecond = 789;
            engine.AddHostObject("bar", HostItemFlags.GlobalMembers, new { second = barSecond });
            engine.AddHostObject("foo", HostItemFlags.GlobalMembers, new { second = fooSecond });
            engine.AddHostObject("foo", HostItemFlags.GlobalMembers, new { first = fooFirst });
        
            
            Assert.AreEqual(fooFirst, engine.Evaluate("first"));
            Assert.AreEqual(barSecond, engine.Evaluate("second"));
            

            var tempFile = System.IO.Path.GetTempFileName();
            engine.WriteHeapSnapshot(tempFile);
            var tempFileInfo =new System.IO.FileInfo(tempFile);
            
            Assert.IsTrue( tempFileInfo.Length >100 );
        }

       

        class TestObj1
        {
            public string AProperty { get; set; }
            public string BProperty { get; set; }
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        [ExpectedException(typeof(ScriptEngineException))]
        public void V8ScriptEngine_AddHostObject_DefaultAccess()
        {
            engine.AddHostObject("test", this);
            engine.Execute("test.PrivateMethod()");
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddHostObject_PrivateAccess()
        {
            engine.AddHostObject("test", HostItemFlags.PrivateAccess, this);
            engine.Execute("test.PrivateMethod()");
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddRestrictedHostObject_BaseClass()
        {
            var host = new ExtendedHostFunctions() as HostFunctions;
            engine.AddRestrictedHostObject("host", host);
            Assert.IsInstanceOfType(engine.Evaluate("host.newObj()"), typeof(PropertyBag));
            TestUtil.AssertException<ScriptEngineException>(() => engine.Evaluate("host.type('System.Int32')"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddRestrictedHostObject_Interface()
        {
            const double value = 123.45;
            engine.AddRestrictedHostObject("convertible", value as IConvertible);
            engine.AddHostObject("culture", CultureInfo.InvariantCulture);
            Assert.AreEqual(value, engine.Evaluate("convertible.ToDouble(culture)"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddHostType()
        {
            engine.AddHostObject("host", new HostFunctions());
            engine.AddHostType("Random", typeof(Random));
            Assert.IsInstanceOfType(engine.Evaluate("host.newObj(Random)"), typeof(Random));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddHostType_GlobalMembers()
        {
            engine.AddHostType("Guid", HostItemFlags.GlobalMembers, typeof(Guid));
            Assert.IsInstanceOfType(engine.Evaluate("NewGuid()"), typeof(Guid));

            engine.AddHostType("Test", HostItemFlags.GlobalMembers, GetType());
            engine.Execute("StaticTestProperty = NewGuid()");
            Assert.IsInstanceOfType(StaticTestProperty, typeof(Guid));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        [ExpectedException(typeof(ScriptEngineException))]
        public void V8ScriptEngine_AddHostType_DefaultAccess()
        {
            engine.AddHostType("Test", GetType());
            engine.Execute("Test.PrivateStaticMethod()");
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddHostType_PrivateAccess()
        {
            engine.AddHostType("Test", HostItemFlags.PrivateAccess, GetType());
            engine.Execute("Test.PrivateStaticMethod()");
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddHostType_Static()
        {
            engine.AddHostType("Enumerable", typeof(Enumerable));
            Assert.IsInstanceOfType(engine.Evaluate("Enumerable.Range(0, 5).ToArray()"), typeof(int[]));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddHostType_OpenGeneric()
        {
            engine.AddHostObject("host", new HostFunctions());
            engine.AddHostType("List", typeof(List<>));
            engine.AddHostType("Guid", typeof(Guid));
            Assert.IsInstanceOfType(engine.Evaluate("host.newObj(List(Guid))"), typeof(List<Guid>));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddHostType_ByName()
        {
            engine.AddHostObject("host", new HostFunctions());
            engine.AddHostType("Random", "System.Random");
            Assert.IsInstanceOfType(engine.Evaluate("host.newObj(Random)"), typeof(Random));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddHostType_ByNameWithAssembly()
        {
            engine.AddHostType("Enumerable", "System.Linq.Enumerable", "System.Core");
            Assert.IsInstanceOfType(engine.Evaluate("Enumerable.Range(0, 5).ToArray()"), typeof(int[]));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddHostType_ByNameWithTypeArgs()
        {
            engine.AddHostObject("host", new HostFunctions());
            engine.AddHostType("Dictionary", "System.Collections.Generic.Dictionary", typeof(string), typeof(int));
            Assert.IsInstanceOfType(engine.Evaluate("host.newObj(Dictionary)"), typeof(Dictionary<string, int>));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddHostType_DefaultName()
        {
            engine.AddHostType(typeof(Random));
            Assert.IsInstanceOfType(engine.Evaluate("new Random()"), typeof(Random));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddHostType_DefaultNameGeneric()
        {
            engine.AddHostType(typeof(List<int>));
            Assert.IsInstanceOfType(engine.Evaluate("new List()"), typeof(List<int>));

            engine.AddHostType(typeof(Dictionary<,>));
            engine.AddHostType(typeof(int));
            engine.AddHostType(typeof(double));
            Assert.IsInstanceOfType(engine.Evaluate("new Dictionary(Int32, Double, 100)"), typeof(Dictionary<int, double>));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Evaluate()
        {
            Assert.AreEqual(Math.E * Math.PI, engine.Evaluate("Math.E * Math.PI"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Evaluate_WithDocumentName()
        {
            const string documentName = "DoTheMath";
            engine.EnableDocumentNameTracking();
            Assert.AreEqual(Math.E * Math.PI, engine.Evaluate(documentName, "Math.E * Math.PI"));
            Assert.IsFalse(engine.GetDocumentNames().Any(name => name.StartsWith(documentName, StringComparison.Ordinal)));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Evaluate_DiscardDocument()
        {
            const string documentName = "DoTheMath";
            engine.EnableDocumentNameTracking();
            Assert.AreEqual(Math.E * Math.PI, engine.Evaluate(documentName, true, "Math.E * Math.PI"));
            Assert.IsFalse(engine.GetDocumentNames().Any(name => name.StartsWith(documentName, StringComparison.Ordinal)));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Evaluate_RetainDocument()
        {
            const string documentName = "DoTheMath";
            engine.EnableDocumentNameTracking();
            Assert.AreEqual(Math.E * Math.PI, engine.Evaluate(documentName, false, "Math.E * Math.PI"));
            Assert.IsTrue(engine.GetDocumentNames().Any(name => name.StartsWith(documentName, StringComparison.Ordinal)));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Execute()
        {
            engine.Execute("epi = Math.E * Math.PI");
            Assert.AreEqual(Math.E * Math.PI, engine.Script.epi);
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Execute_WithDocumentName()
        {
            const string documentName = "DoTheMath";
            engine.EnableDocumentNameTracking();
            engine.Execute(documentName, "epi = Math.E * Math.PI");
            Assert.AreEqual(Math.E * Math.PI, engine.Script.epi);
            Assert.IsTrue(engine.GetDocumentNames().Any(name => name.StartsWith(documentName, StringComparison.Ordinal)));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Execute_DiscardDocument()
        {
            const string documentName = "DoTheMath";
            engine.EnableDocumentNameTracking();
            engine.Execute(documentName, true, "epi = Math.E * Math.PI");
            Assert.AreEqual(Math.E * Math.PI, engine.Script.epi);
            Assert.IsFalse(engine.GetDocumentNames().Any(name => name.StartsWith(documentName, StringComparison.Ordinal)));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Execute_RetainDocument()
        {
            const string documentName = "DoTheMath";
            engine.EnableDocumentNameTracking();
            engine.Execute(documentName, false, "epi = Math.E * Math.PI");
            Assert.AreEqual(Math.E * Math.PI, engine.Script.epi);
            Assert.IsTrue(engine.GetDocumentNames().Any(name => name.StartsWith(documentName, StringComparison.Ordinal)));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Execute_CompiledScript()
        {
            using (var script = engine.Compile("epi = Math.E * Math.PI"))
            {
                engine.Execute(script);
                Assert.AreEqual(Math.E * Math.PI, engine.Script.epi);
            }
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_ExecuteCommand_EngineConvert()
        {
            Assert.AreEqual("[object Math]", engine.ExecuteCommand("Math"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_ExecuteCommand_HostConvert()
        {
            var dateHostItem = HostItem.Wrap(engine, new DateTime(2007, 5, 22, 6, 15, 43));
            engine.AddHostObject("date", dateHostItem);
            Assert.AreEqual(dateHostItem.ToString(), engine.ExecuteCommand("date"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_ExecuteCommand_var()
        {
            Assert.AreEqual("[undefined]", engine.ExecuteCommand("var x = 'foo'"));
            Assert.AreEqual("foo", engine.Script.x);
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_ExecuteCommand_HostVariable()
        {
            engine.Script.host = new HostFunctions();
            Assert.AreEqual("[HostVariable:String]", engine.ExecuteCommand("host.newVar('foo')"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Invoke_ScriptFunction()
        {
            engine.Execute("function foo(x) { return x * Math.PI; }");
            Assert.AreEqual(Math.E * Math.PI, engine.Invoke("foo", Math.E));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Invoke_HostDelegate()
        {
            engine.Script.foo = new Func<double, double>(x => x * Math.PI);
            Assert.AreEqual(Math.E * Math.PI, engine.Invoke("foo", Math.E));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Interrupt()
        {
            var checkpoint = new ManualResetEvent(false);
            ThreadPool.QueueUserWorkItem(state =>
            {
                checkpoint.WaitOne();
                engine.Interrupt();
            });

            engine.AddHostObject("checkpoint", checkpoint);

            // V8 can't interrupt code that accesses only native data
            engine.AddHostObject("test", new { foo = "bar" });

            TestUtil.AssertException<OperationCanceledException>(() => engine.Execute("checkpoint.Set(); while (true) { var foo = test.foo; }"));
            Assert.AreEqual(Math.E * Math.PI, engine.Evaluate("Math.E * Math.PI"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        [ExpectedException(typeof(ScriptEngineException))]
        public void V8ScriptEngine_AccessContext_Default()
        {
            engine.AddHostObject("test", this);
            engine.Execute("test.PrivateMethod()");
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AccessContext_Private()
        {
            engine.AddHostObject("test", this);
            engine.AccessContext = GetType();
            engine.Execute("test.PrivateMethod()");
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_ContinuationCallback()
        {
            // V8 can't interrupt code that accesses only native data
            engine.AddHostObject("test", new { foo = "bar" });

            engine.ContinuationCallback = () => false;
            TestUtil.AssertException<OperationCanceledException>(() => engine.Execute("while (true) { var foo = test.foo; }"));
            engine.ContinuationCallback = null;
            Assert.AreEqual(Math.E * Math.PI, engine.Evaluate("Math.E * Math.PI"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_FileNameExtension()
        {
            Assert.AreEqual("js", engine.FileNameExtension);
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Script_Property()
        {
            var host = new HostFunctions();
            engine.Script.host = host;
            Assert.AreSame(host, engine.Script.host);
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Script_Property_Scalar()
        {
            const int value = 123;
            engine.Script.value = value;
            Assert.AreEqual(value, engine.Script.value);
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Script_Property_Enum()
        {
            const DayOfWeek value = DayOfWeek.Wednesday;
            engine.Script.value = value;
            Assert.AreEqual(value, engine.Script.value);
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Script_Property_Struct()
        {
            var date = new DateTime(2007, 5, 22, 6, 15, 43);
            engine.Script.date = date;
            Assert.AreEqual(date, engine.Script.date);
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Script_Index_ArrayItem()
        {
            const int index = 5;
            engine.Execute("foo = []");

            engine.Script.foo[index] = engine.Script.Math.PI;
            Assert.AreEqual(Math.PI, engine.Script.foo[index]);
            Assert.AreEqual(index + 1, engine.Evaluate("foo.length"));

            engine.Script.foo[index] = engine.Script.Math.E;
            Assert.AreEqual(Math.E, engine.Script.foo[index]);
            Assert.AreEqual(index + 1, engine.Evaluate("foo.length"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Script_Index_Property()
        {
            const string name = "bar";
            engine.Execute("foo = {}");

            engine.Script.foo[name] = engine.Script.Math.PI;
            Assert.AreEqual(Math.PI, engine.Script.foo[name]);
            Assert.AreEqual(Math.PI, engine.Script.foo.bar);

            engine.Script.foo[name] = engine.Script.Math.E;
            Assert.AreEqual(Math.E, engine.Script.foo[name]);
            Assert.AreEqual(Math.E, engine.Script.foo.bar);
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Script_Method()
        {
            engine.Execute("function foo(x) { return x * x; }");
            Assert.AreEqual(25, engine.Script.foo(5));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Script_Method_Intrinsic()
        {
            Assert.AreEqual(Math.E * Math.PI, engine.Script.eval("Math.E * Math.PI"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Script_Property_VB()
        {
            TestUtil.InvokeVBTestSub(@"
                Using engine As New V8ScriptEngine
                    Dim host As New HostFunctions
                    engine.Script.host = host
                    Assert.AreSame(host, engine.Script.host)
                End Using
            ");
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Script_Property_Scalar_VB()
        {
            TestUtil.InvokeVBTestSub(@"
                Using engine As New V8ScriptEngine
                    Dim value = 123
                    engine.Script.value = value
                    Assert.AreEqual(value, engine.Script.value)
                End Using
            ");
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Script_Property_Enum_VB()
        {
            TestUtil.InvokeVBTestSub(@"
                Using engine As New V8ScriptEngine
                    Dim value = DayOfWeek.Wednesday
                    engine.Script.value = value
                    Assert.AreEqual(value, engine.Script.value)
                End Using
            ");
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Script_Property_Struct_VB()
        {
            TestUtil.InvokeVBTestSub(@"
                Using engine As New V8ScriptEngine
                    Dim value As New DateTime(2007, 5, 22, 6, 15, 43)
                    engine.Script.value = value
                    Assert.AreEqual(value, engine.Script.value)
                End Using
            ");
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Script_Index_ArrayItem_VB()
        {
            TestUtil.InvokeVBTestSub(@"
                Using engine As New V8ScriptEngine

                    Dim index = 5
                    engine.Execute(""foo = []"")

                    engine.Script.foo(index) = engine.Script.Math.PI
                    Assert.AreEqual(Math.PI, engine.Script.foo(index))
                    Assert.AreEqual(index + 1, engine.Evaluate(""foo.length""))

                    engine.Script.foo(index) = engine.Script.Math.E
                    Assert.AreEqual(Math.E, engine.Script.foo(index))
                    Assert.AreEqual(index + 1, engine.Evaluate(""foo.length""))

                End Using
            ");
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Script_Index_Property_VB()
        {
            TestUtil.InvokeVBTestSub(@"
                Using engine As New V8ScriptEngine

                    Dim name = ""bar""
                    engine.Execute(""foo = {}"")

                    engine.Script.foo(name) = engine.Script.Math.PI
                    Assert.AreEqual(Math.PI, engine.Script.foo(name))
                    Assert.AreEqual(Math.PI, engine.Script.foo.bar)

                    engine.Script.foo(name) = engine.Script.Math.E
                    Assert.AreEqual(Math.E, engine.Script.foo(name))
                    Assert.AreEqual(Math.E, engine.Script.foo.bar)

                End Using
            ");
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Script_Method_VB()
        {
            TestUtil.InvokeVBTestSub(@"
                Using engine As New V8ScriptEngine
                    engine.Execute(""function foo(x) { return x * x; }"")
                    Assert.AreEqual(25, engine.Script.foo(5))
                End Using
            ");
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Script_Method_Intrinsic_VB()
        {
            TestUtil.InvokeVBTestSub(@"
                Using engine As New V8ScriptEngine
                    Assert.AreEqual(Math.E * Math.PI, engine.Script.eval(""Math.E * Math.PI""))
                End Using
            ");
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_CollectGarbage()
        {
            engine.Execute(@"x = []; for (i = 0; i < 1024 * 1024; i++) { x.push(x); }");
            var usedHeapSize = engine.GetRuntimeHeapInfo().UsedHeapSize;
            engine.CollectGarbage(true);
            Assert.IsTrue(usedHeapSize > engine.GetRuntimeHeapInfo().UsedHeapSize);
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_CollectGarbage_HostObject()
        {
            // ReSharper disable RedundantAssignment

            var x = new object();
            var wr = new WeakReference(x);
            engine.Script.x = x;

            x = null;
            engine.Script.x = null;

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
            Assert.IsTrue(wr.IsAlive);

            engine.CollectGarbage(true);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
            Assert.IsFalse(wr.IsAlive);

            // ReSharper restore RedundantAssignment
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Parallel()
        {
            engine.AddHostObject("host", new HostFunctions());
            engine.AddHostObject("clr", HostItemFlags.GlobalMembers, new HostTypeCollection("mscorlib"));

            const int threadCount = 256;
            engine.AddHostObject("list", Enumerable.Range(0, threadCount).ToList());
            Assert.AreEqual(threadCount, engine.Evaluate("list.Count"));

            var startEvent = new ManualResetEventSlim(false);
            var stopEvent = new ManualResetEventSlim(false);
            engine.AddHostObject("stopEvent", stopEvent);

            ThreadStart body = () =>
            {
                // ReSharper disable AccessToDisposedClosure

                startEvent.Wait();
                engine.Execute("list.RemoveAt(0); if (list.Count == 0) { stopEvent.Set(); }");

                // ReSharper restore AccessToDisposedClosure
            };

            var threads = Enumerable.Range(0, threadCount).Select(index => new Thread(body)).ToArray();
            threads.ForEach(thread => thread.Start());

            startEvent.Set();
            stopEvent.Wait();
            Assert.AreEqual(0, engine.Evaluate("list.Count"));

            threads.ForEach(thread => thread.Join());
            startEvent.Dispose();
            stopEvent.Dispose();
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_new()
        {
            engine.AddHostObject("clr", HostItemFlags.GlobalMembers, new HostTypeCollection("mscorlib"));
            Assert.IsInstanceOfType(engine.Evaluate("new System.Random()"), typeof(Random));
            Assert.IsInstanceOfType(engine.Evaluate("new System.Random(100)"), typeof(Random));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_new_Generic()
        {
            engine.AddHostObject("clr", HostItemFlags.GlobalMembers, new HostTypeCollection("mscorlib"));
            Assert.IsInstanceOfType(engine.Evaluate("new System.Collections.Generic.Dictionary(System.Int32, System.String)"), typeof(Dictionary<int, string>));
            Assert.IsInstanceOfType(engine.Evaluate("new System.Collections.Generic.Dictionary(System.Int32, System.String, 100)"), typeof(Dictionary<int, string>));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_new_GenericNested()
        {
            engine.AddHostObject("clr", HostItemFlags.GlobalMembers, new HostTypeCollection("mscorlib", "System.Core"));
            engine.AddHostObject("dict", new Dictionary<int, string> { { 12345, "foo" }, { 54321, "bar" } });
            Assert.IsInstanceOfType(engine.Evaluate("vc = new (System.Collections.Generic.Dictionary(System.Int32, System.String).ValueCollection)(dict)"), typeof(Dictionary<int, string>.ValueCollection));
            Assert.IsTrue((bool)engine.Evaluate("vc.SequenceEqual(dict.Values)"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_new_Scalar()
        {
            engine.AddHostObject("clr", HostItemFlags.GlobalMembers, new HostTypeCollection("mscorlib"));
            Assert.AreEqual(default(int), engine.Evaluate("new System.Int32"));
            Assert.AreEqual(default(int), engine.Evaluate("new System.Int32()"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_new_Enum()
        {
            engine.AddHostObject("clr", HostItemFlags.GlobalMembers, new HostTypeCollection("mscorlib"));
            Assert.AreEqual(default(DayOfWeek), engine.Evaluate("new System.DayOfWeek"));
            Assert.AreEqual(default(DayOfWeek), engine.Evaluate("new System.DayOfWeek()"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_new_Struct()
        {
            engine.AddHostObject("clr", HostItemFlags.GlobalMembers, new HostTypeCollection("mscorlib"));
            Assert.AreEqual(default(DateTime), engine.Evaluate("new System.DateTime"));
            Assert.AreEqual(default(DateTime), engine.Evaluate("new System.DateTime()"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_new_NoMatch()
        {
            engine.AddHostObject("clr", HostItemFlags.GlobalMembers, new HostTypeCollection("mscorlib"));
            TestUtil.AssertException<MissingMemberException>(() => engine.Execute("new System.Random('a')"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_General()
        {
            using (var console = new StringWriter())
            {
                var clr = new HostTypeCollection(type => type != typeof(Console), "mscorlib", "System", "System.Core");
                clr.GetNamespaceNode("System").SetPropertyNoCheck("Console", console);

                engine.AddHostObject("host", new ExtendedHostFunctions());
                engine.AddHostObject("clr", clr);

                engine.Execute(generalScript);
                Assert.AreEqual(MiscHelpers.FormatCode(generalScriptOutput), console.ToString().Replace("\r\n", "\n"));
            }
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_General_Precompiled()
        {
            using (var script = engine.Compile(generalScript))
            {
                using (var console = new StringWriter())
                {
                    var clr = new HostTypeCollection(type => type != typeof(Console), "mscorlib", "System", "System.Core");
                    clr.GetNamespaceNode("System").SetPropertyNoCheck("Console", console);

                    engine.AddHostObject("host", new ExtendedHostFunctions());
                    engine.AddHostObject("clr", clr);

                    engine.Evaluate(script);
                    Assert.AreEqual(MiscHelpers.FormatCode(generalScriptOutput), console.ToString().Replace("\r\n", "\n"));

                    console.GetStringBuilder().Clear();
                    Assert.AreEqual(string.Empty, console.ToString());

                    engine.Evaluate(script);
                    Assert.AreEqual(MiscHelpers.FormatCode(generalScriptOutput), console.ToString().Replace("\r\n", "\n"));
                }
            }
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_General_Precompiled_Dual()
        {
            engine.Dispose();
            using (var runtime = new V8Runtime())
            {
                using (var script = runtime.Compile(generalScript))
                {
                    engine = runtime.CreateScriptEngine();
                    using (var console = new StringWriter())
                    {
                        var clr = new HostTypeCollection(type => type != typeof(Console), "mscorlib", "System", "System.Core");
                        clr.GetNamespaceNode("System").SetPropertyNoCheck("Console", console);

                        engine.AddHostObject("host", new ExtendedHostFunctions());
                        engine.AddHostObject("clr", clr);

                        engine.Evaluate(script);
                        Assert.AreEqual(MiscHelpers.FormatCode(generalScriptOutput), console.ToString().Replace("\r\n", "\n"));

                        console.GetStringBuilder().Clear();
                        Assert.AreEqual(string.Empty, console.ToString());

                        engine.Evaluate(script);
                        Assert.AreEqual(MiscHelpers.FormatCode(generalScriptOutput), console.ToString().Replace("\r\n", "\n"));
                    }

                    engine.Dispose();
                    engine = runtime.CreateScriptEngine();
                    using (var console = new StringWriter())
                    {
                        var clr = new HostTypeCollection(type => type != typeof(Console), "mscorlib", "System", "System.Core");
                        clr.GetNamespaceNode("System").SetPropertyNoCheck("Console", console);

                        engine.AddHostObject("host", new ExtendedHostFunctions());
                        engine.AddHostObject("clr", clr);

                        engine.Evaluate(script);
                        Assert.AreEqual(MiscHelpers.FormatCode(generalScriptOutput), console.ToString().Replace("\r\n", "\n"));

                        console.GetStringBuilder().Clear();
                        Assert.AreEqual(string.Empty, console.ToString());

                        engine.Evaluate(script);
                        Assert.AreEqual(MiscHelpers.FormatCode(generalScriptOutput), console.ToString().Replace("\r\n", "\n"));
                    }
                }
            }
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_General_Precompiled_Execute()
        {
            using (var script = engine.Compile(generalScript))
            {
                using (var console = new StringWriter())
                {
                    var clr = new HostTypeCollection(type => type != typeof(Console), "mscorlib", "System", "System.Core");
                    clr.GetNamespaceNode("System").SetPropertyNoCheck("Console", console);

                    engine.AddHostObject("host", new ExtendedHostFunctions());
                    engine.AddHostObject("clr", clr);

                    engine.Execute(script);
                    Assert.AreEqual(MiscHelpers.FormatCode(generalScriptOutput), console.ToString().Replace("\r\n", "\n"));

                    console.GetStringBuilder().Clear();
                    Assert.AreEqual(string.Empty, console.ToString());

                    engine.Execute(script);
                    Assert.AreEqual(MiscHelpers.FormatCode(generalScriptOutput), console.ToString().Replace("\r\n", "\n"));
                }
            }
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_ErrorHandling_SyntaxError()
        {
            TestUtil.AssertException<ScriptEngineException>(() =>
            {
                try
                {
                    engine.Execute("function foo() { int c; }");
                }
                catch (ScriptEngineException exception)
                {
                    TestUtil.AssertValidException(engine, exception);
                    Assert.IsNull(exception.InnerException);
                    Assert.IsTrue(exception.Message.Contains("SyntaxError"));
                    Assert.IsTrue(exception.ErrorDetails.Contains(" -> "));
                    throw;
                }
            });
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_ErrorHandling_ThrowNonError()
        {
            TestUtil.AssertException<ScriptEngineException>(() =>
            {
                try
                {
                    engine.Execute("(function () { throw 123; })()");
                }
                catch (ScriptEngineException exception)
                {
                    TestUtil.AssertValidException(engine, exception);
                    Assert.IsNull(exception.InnerException);
                    Assert.IsTrue(exception.Message.StartsWith("123", StringComparison.Ordinal));
                    Assert.IsTrue(exception.ErrorDetails.Contains(" -> "));
                    throw;
                }
            });
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_ErrorHandling_ScriptError()
        {
            TestUtil.AssertException<ScriptEngineException>(() =>
            {
                try
                {
                    engine.Execute("foo = {}; foo();");
                }
                catch (ScriptEngineException exception)
                {
                    TestUtil.AssertValidException(engine, exception);
                    Assert.IsNull(exception.InnerException);
                    throw;
                }
            });
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_ErrorHandling_HostException()
        {
            engine.AddHostObject("host", new HostFunctions());

            TestUtil.AssertException<ScriptEngineException>(() =>
            {
                try
                {
                    engine.Evaluate("host.proc(0)");
                }
                catch (ScriptEngineException exception)
                {
                    TestUtil.AssertValidException(engine, exception);
                    Assert.IsNotNull(exception.InnerException);

                    var hostException = exception.InnerException;
                    Assert.IsInstanceOfType(hostException, typeof(RuntimeBinderException));
                    TestUtil.AssertValidException(hostException);
                    Assert.IsNull(hostException.InnerException);

                    Assert.AreEqual("Error: " + hostException.Message, exception.Message);
                    throw;
                }
            });
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_ErrorHandling_IgnoredHostException()
        {
            engine.AddHostObject("host", new HostFunctions());

            TestUtil.AssertException<ScriptEngineException>(() =>
            {
                try
                {
                    engine.Execute("try { host.newObj(null); } catch(ex) {} foo = {}; foo();");
                }
                catch (ScriptEngineException exception)
                {
                    TestUtil.AssertValidException(engine, exception);
                    Assert.IsNull(exception.InnerException);
                    throw;
                }
            });
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_ErrorHandling_NestedScriptError()
        {
            using (var innerEngine = new V8ScriptEngine("inner", V8ScriptEngineFlags.EnableDebugging))
            {
                engine.AddHostObject("engine", innerEngine);

                TestUtil.AssertException<ScriptEngineException>(() =>
                {
                    try
                    {
                        engine.Execute("engine.Execute('foo = {}; foo();')");
                    }
                    catch (ScriptEngineException exception)
                    {
                        TestUtil.AssertValidException(engine, exception);
                        Assert.IsNotNull(exception.InnerException);

                        var hostException = exception.InnerException;
                        Assert.IsInstanceOfType(hostException, typeof(TargetInvocationException));
                        TestUtil.AssertValidException(hostException);
                        Assert.IsNotNull(hostException.InnerException);

                        var nestedException = hostException.InnerException as ScriptEngineException;
                        Assert.IsNotNull(nestedException);
                        // ReSharper disable once AccessToDisposedClosure
                        TestUtil.AssertValidException(innerEngine, nestedException);
                        // ReSharper disable once PossibleNullReferenceException
                        Assert.IsNull(nestedException.InnerException);

                        Assert.AreEqual("Error: " + hostException.GetBaseException().Message, exception.Message);
                        throw;
                    }
                });
            }
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_ErrorHandling_NestedHostException()
        {
            using (var innerEngine = new V8ScriptEngine("inner", V8ScriptEngineFlags.EnableDebugging))
            {
                innerEngine.AddHostObject("host", new HostFunctions());
                engine.AddHostObject("engine", innerEngine);

                TestUtil.AssertException<ScriptEngineException>(() =>
                {
                    try
                    {
                        engine.Execute("engine.Evaluate('host.proc(0)')");
                    }
                    catch (ScriptEngineException exception)
                    {
                        TestUtil.AssertValidException(engine, exception);
                        Assert.IsNotNull(exception.InnerException);

                        var hostException = exception.InnerException;
                        Assert.IsInstanceOfType(hostException, typeof(TargetInvocationException));
                        TestUtil.AssertValidException(hostException);
                        Assert.IsNotNull(hostException.InnerException);

                        var nestedException = hostException.InnerException as ScriptEngineException;
                        Assert.IsNotNull(nestedException);
                        // ReSharper disable once AccessToDisposedClosure
                        TestUtil.AssertValidException(innerEngine, nestedException);
                        // ReSharper disable once PossibleNullReferenceException
                        Assert.IsNotNull(nestedException.InnerException);

                        var nestedHostException = nestedException.InnerException;
                        Assert.IsInstanceOfType(nestedHostException, typeof(RuntimeBinderException));
                        TestUtil.AssertValidException(nestedHostException);
                        Assert.IsNull(nestedHostException.InnerException);

                        Assert.AreEqual("Error: " + nestedHostException.Message, nestedException.Message);
                        Assert.AreEqual("Error: " + hostException.GetBaseException().Message, exception.Message);
                        throw;
                    }
                });
            }
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_MaxRuntimeHeapSize()
        {
            const int limit = 4 * 1024 * 1024;
            const string code = @"x = []; while (true) { x.push(x); }";

            engine.MaxRuntimeHeapSize = (UIntPtr)limit;

            TestUtil.AssertException<ScriptEngineException>(() =>
            {
                try
                {
                    engine.Execute(code);
                }
                catch (ScriptEngineException exception)
                {
                    Assert.IsTrue(exception.IsFatal);
                    throw;
                }
            });

            TestUtil.AssertException<ScriptEngineException>(() =>
            {
                try
                {
                    engine.CollectGarbage(true);
                    engine.Execute("x = 5");
                }
                catch (ScriptEngineException exception)
                {
                    Assert.IsTrue(exception.IsFatal);
                    throw;
                }
            });
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_MaxRuntimeHeapSize_Recovery()
        {
            const int limit = 4 * 1024 * 1024;
            const string code = @"x = []; while (true) { x.push(x); }";

            engine.MaxRuntimeHeapSize = (UIntPtr)limit;

            TestUtil.AssertException<ScriptEngineException>(() =>
            {
                try
                {
                    engine.Execute(code);
                }
                catch (ScriptEngineException exception)
                {
                    Assert.IsTrue(exception.IsFatal);
                    throw;
                }
            });

            engine.MaxRuntimeHeapSize = (UIntPtr)(limit * 16);
            engine.Execute("x = 5");
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_MaxRuntimeHeapSize_Dual()
        {
            const int limit = 4 * 1024 * 1024;
            const string code = @"x = []; for (i = 0; i < 8 * 1024 * 1024; i++) { x.push(x); }";

            engine.Execute(code);
            engine.CollectGarbage(true);
            var usedHeapSize = engine.GetRuntimeHeapInfo().UsedHeapSize;

            engine.Dispose();
            engine = new V8ScriptEngine { MaxRuntimeHeapSize = (UIntPtr)limit };

            TestUtil.AssertException<ScriptEngineException>(() =>
            {
                try
                {
                    engine.Execute(code);
                }
                catch (ScriptEngineException exception)
                {
                    Assert.IsTrue(exception.IsFatal);
                    throw;
                }
            });

            engine.CollectGarbage(true);
            Assert.IsTrue(usedHeapSize > engine.GetRuntimeHeapInfo().UsedHeapSize);
        }






          



        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_MaxRuntimeHeapSize_ShortBursts()
        {
            const int limit = 4 * 1024 * 1024;
            const string code = @"for (i = 0; i < 1024 * 1024; i++) { x.push(x); }";

            engine.MaxRuntimeHeapSize = (UIntPtr)limit;
            engine.RuntimeHeapSizeSampleInterval = TimeSpan.FromMilliseconds(30000);

            TestUtil.AssertException<ScriptEngineException>(() =>
            {
                try
                {
                    engine.Execute("x = []");
                    using (var script = engine.Compile(code))
                    {
                        while (true)
                        {
                            engine.Evaluate(script);
                        }
                    }
                }
                catch (ScriptEngineException exception)
                {
                    Assert.IsTrue(exception.IsFatal);
                    throw;
                }
            });
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_CreateInstance()
        {
            engine.Script.testObject = new DynamicTestObject();
            Assert.AreEqual("foo bar baz qux", engine.Evaluate("new testObject('foo', 'bar', 'baz', 'qux')"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_CreateInstance_Fail()
        {
            engine.Script.testObject = new DynamicTestObject();
            TestUtil.AssertException<InvalidOperationException>(() => engine.Evaluate("new testObject()"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_Invoke()
        {
            engine.Script.testObject = new DynamicTestObject();
            Assert.AreEqual("foo,bar,baz,qux", engine.Evaluate("testObject('foo', 'bar', 'baz', 'qux')"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_Invoke_Fail()
        {
            engine.Script.testObject = new DynamicTestObject();
            TestUtil.AssertException<InvalidOperationException>(() => engine.Evaluate("testObject()"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_InvokeMethod()
        {
            engine.Script.testObject = new DynamicTestObject();
            Assert.AreEqual("foo-bar-baz-qux", engine.Evaluate("testObject.DynamicMethod('foo', 'bar', 'baz', 'qux')"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_InvokeMethod_Fail()
        {
            engine.Script.testObject = new DynamicTestObject();
            TestUtil.AssertException<MissingMemberException>(() => engine.Evaluate("testObject.DynamicMethod()"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_InvokeMethod_FieldOverride()
        {
            engine.Script.testObject = new DynamicTestObject();
            Assert.AreEqual("foo.bar.baz.qux", engine.Evaluate("testObject.SomeField('foo', 'bar', 'baz', 'qux')"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_InvokeMethod_FieldOverride_Fail()
        {
            engine.Script.testObject = new DynamicTestObject();
            TestUtil.AssertException<MissingMemberException>(() => engine.Evaluate("testObject.SomeField()"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_InvokeMethod_PropertyOverride()
        {
            engine.Script.testObject = new DynamicTestObject();
            Assert.AreEqual("foo:bar:baz:qux", engine.Evaluate("testObject.SomeProperty('foo', 'bar', 'baz', 'qux')"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_InvokeMethod_PropertyOverride_Fail()
        {
            engine.Script.testObject = new DynamicTestObject();
            TestUtil.AssertException<MissingMemberException>(() => engine.Evaluate("testObject.SomeProperty()"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_InvokeMethod_DynamicOverload()
        {
            engine.Script.testObject = new DynamicTestObject();
            Assert.AreEqual("foo;bar;baz;qux", engine.Evaluate("testObject.SomeMethod('foo', 'bar', 'baz', 'qux')"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_InvokeMethod_NonDynamicOverload()
        {
            engine.Script.testObject = new DynamicTestObject();
            Assert.AreEqual(Math.PI, engine.Evaluate("testObject.SomeMethod()"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_InvokeMethod_NonDynamic()
        {
            engine.Script.testObject = new DynamicTestObject();
            Assert.AreEqual("Super Bass-O-Matic '76", engine.Evaluate("testObject.ToString()"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_StaticType_Field()
        {
            engine.Script.testObject = new DynamicTestObject();
            engine.Script.host = new HostFunctions();
            Assert.IsInstanceOfType(engine.Evaluate("testObject.SomeField"), typeof(HostMethod));
            Assert.AreEqual(12345, engine.Evaluate("host.toStaticType(testObject).SomeField"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_StaticType_Property()
        {
            engine.Script.testObject = new DynamicTestObject();
            engine.Script.host = new HostFunctions();
            Assert.IsInstanceOfType(engine.Evaluate("testObject.SomeProperty"), typeof(HostMethod));
            Assert.AreEqual("Bogus", engine.Evaluate("host.toStaticType(testObject).SomeProperty"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_StaticType_Method()
        {
            engine.Script.testObject = new DynamicTestObject();
            engine.Script.host = new HostFunctions();
            Assert.AreEqual("bar+baz+qux", engine.Evaluate("host.toStaticType(testObject).SomeMethod('foo', 'bar', 'baz', 'qux')"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_Property()
        {
            engine.Script.testObject = new DynamicTestObject();
            Assert.IsInstanceOfType(engine.Evaluate("testObject.foo"), typeof(Undefined));
            Assert.AreEqual(123, engine.Evaluate("testObject.foo = 123"));
            Assert.AreEqual(123, engine.Evaluate("testObject.foo"));
            Assert.IsTrue((bool)engine.Evaluate("delete testObject.foo"));
            Assert.IsInstanceOfType(engine.Evaluate("testObject.foo"), typeof(Undefined));
            Assert.IsFalse((bool)engine.Evaluate("delete testObject.foo"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_Property_Fail()
        {
            engine.Script.testObject = new DynamicTestObject();
            Assert.IsInstanceOfType(engine.Evaluate("testObject.Zfoo"), typeof(Undefined));
            TestUtil.AssertException<MissingMemberException>(() => engine.Evaluate("testObject.Zfoo = 123"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_Property_Invoke()
        {
            engine.Script.testObject = new DynamicTestObject();
            Assert.IsInstanceOfType(engine.Evaluate("testObject.foo"), typeof(Undefined));
            Assert.IsInstanceOfType(engine.Evaluate("testObject.foo = function(x) { return x.length; }"), typeof(DynamicObject));
            Assert.AreEqual("floccinaucinihilipilification".Length, engine.Evaluate("testObject.foo('floccinaucinihilipilification')"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_Property_Invoke_Nested()
        {
            engine.Script.testObject = new DynamicTestObject();
            Assert.IsInstanceOfType(engine.Evaluate("testObject.foo"), typeof(Undefined));
            Assert.IsInstanceOfType(engine.Evaluate("testObject.foo = testObject"), typeof(DynamicTestObject));
            Assert.AreEqual("foo,bar,baz,qux", engine.Evaluate("testObject.foo('foo', 'bar', 'baz', 'qux')"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_Element()
        {
            engine.Script.testObject = new DynamicTestObject();
            engine.Script.host = new HostFunctions();
            Assert.IsInstanceOfType(engine.Evaluate("host.getElement(testObject, 1, 2, 3, 'foo')"), typeof(Undefined));
            Assert.AreEqual("bar", engine.Evaluate("host.setElement(testObject, 'bar', 1, 2, 3, 'foo')"));
            Assert.AreEqual("bar", engine.Evaluate("host.getElement(testObject, 1, 2, 3, 'foo')"));
            Assert.IsTrue((bool)engine.Evaluate("host.removeElement(testObject, 1, 2, 3, 'foo')"));
            Assert.IsInstanceOfType(engine.Evaluate("host.getElement(testObject, 1, 2, 3, 'foo')"), typeof(Undefined));
            Assert.IsFalse((bool)engine.Evaluate("host.removeElement(testObject, 1, 2, 3, 'foo')"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_Element_Fail()
        {
            engine.Script.testObject = new DynamicTestObject();
            engine.Script.host = new HostFunctions();
            Assert.IsInstanceOfType(engine.Evaluate("host.getElement(testObject, 1, 2, 3, Math.PI)"), typeof(Undefined));
            TestUtil.AssertException<InvalidOperationException>(() => engine.Evaluate("host.setElement(testObject, 'bar', 1, 2, 3, Math.PI)"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DynamicHostObject_Element_Convert()
        {
            engine.Script.testObject = new DynamicTestObject();
            engine.Script.host = new HostFunctions();
            engine.AddHostType("int_t", typeof(int));
            engine.AddHostType("string_t", typeof(string));
            Assert.AreEqual(98765, engine.Evaluate("host.cast(int_t, testObject)"));
            Assert.AreEqual("Booyakasha!", engine.Evaluate("host.cast(string_t, testObject)"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_HostIndexers()
        {
            engine.Script.testObject = new TestObject();

            TestUtil.AssertException<KeyNotFoundException>(() => engine.Evaluate("testObject.Item(123)"));
            TestUtil.AssertException<KeyNotFoundException>(() => engine.Evaluate("testObject.Item.get(123)"));
            Assert.AreEqual(Math.E, engine.Evaluate("testObject.Item.set(123, Math.E)"));
            Assert.AreEqual(Math.E, engine.Evaluate("testObject.Item.get(123)"));

            TestUtil.AssertException<KeyNotFoundException>(() => engine.Evaluate("testObject.Item('456')"));
            TestUtil.AssertException<KeyNotFoundException>(() => engine.Evaluate("testObject.Item.get('456')"));
            Assert.AreEqual(Math.Sqrt(3), engine.Evaluate("testObject.Item.set('456', Math.sqrt(3))"));
            Assert.AreEqual(Math.Sqrt(3), engine.Evaluate("testObject.Item.get('456')"));

            TestUtil.AssertException<KeyNotFoundException>(() => engine.Evaluate("testObject.Item(123, '456', 789.987, -0.12345)"));
            TestUtil.AssertException<KeyNotFoundException>(() => engine.Evaluate("testObject.Item.get(123, '456', 789.987, -0.12345)"));
            Assert.AreEqual(Math.Sqrt(7), engine.Evaluate("testObject.Item.set(123, '456', 789.987, -0.12345, Math.sqrt(7))"));
            Assert.AreEqual(Math.Sqrt(7), engine.Evaluate("testObject.Item.get(123, '456', 789.987, -0.12345)"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_FormatCode()
        {
            try
            {
                engine.Execute("a", "\n\n\n     x = 3.a");
            }
            catch (ScriptEngineException exception)
            {
                Assert.IsTrue(exception.ErrorDetails.Contains(" a:4:10 "));
            }

            engine.FormatCode = true;
            try
            {
                engine.Execute("b", "\n\n\n     x = 3.a");
            }
            catch (ScriptEngineException exception)
            {
                Assert.IsTrue(exception.ErrorDetails.Contains(" b:1:5 "));
            }
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_GetStackTrace()
        {
            engine.AddHostObject("qux", new Func<object>(() => engine.GetStackTrace()));
            engine.Execute(@"
                function baz() { return qux(); }
                function bar() { return baz(); }
                function foo() { return bar(); }
            ");

            Assert.AreEqual("    at baz (Script Document:2:41)\n    at bar (Script Document:3:41)\n    at foo (Script Document:4:41)\n    at Script Document [2] [temp]:1:1", engine.Evaluate("foo()"));
            Assert.AreEqual("    at baz (Script Document:2:41)\n    at bar (Script Document:3:41)\n    at foo (Script Document:4:41)", engine.Script.foo());
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_MaxRuntimeHeapSize_Plumbing()
        {
            using (var runtime = new V8Runtime())
            {
                using (var engine1 = runtime.CreateScriptEngine())
                {
                    using (var engine2 = runtime.CreateScriptEngine())
                    {
                        var value = (UIntPtr)123456;
                        engine1.MaxRuntimeHeapSize = value;
                        Assert.AreEqual(value, engine1.MaxRuntimeHeapSize);
                        Assert.AreEqual(value, engine2.MaxRuntimeHeapSize);
                        Assert.AreEqual(value, runtime.MaxHeapSize);
                        Assert.AreEqual(UIntPtr.Zero, engine1.MaxRuntimeStackUsage);
                        Assert.AreEqual(UIntPtr.Zero, engine2.MaxRuntimeStackUsage);
                        Assert.AreEqual(UIntPtr.Zero, runtime.MaxStackUsage);

                        value = (UIntPtr)654321;
                        runtime.MaxHeapSize = value;
                        Assert.AreEqual(value, engine1.MaxRuntimeHeapSize);
                        Assert.AreEqual(value, engine2.MaxRuntimeHeapSize);
                        Assert.AreEqual(value, runtime.MaxHeapSize);
                        Assert.AreEqual(UIntPtr.Zero, engine1.MaxRuntimeStackUsage);
                        Assert.AreEqual(UIntPtr.Zero, engine2.MaxRuntimeStackUsage);
                        Assert.AreEqual(UIntPtr.Zero, runtime.MaxStackUsage);
                    }
                }
            }
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_RuntimeHeapSizeSampleInterval_Plumbing()
        {
            using (var runtime = new V8Runtime())
            {
                using (var engine1 = runtime.CreateScriptEngine())
                {
                    using (var engine2 = runtime.CreateScriptEngine())
                    {
                        var value = TimeSpan.FromMilliseconds(123456789.0);
                        engine1.RuntimeHeapSizeSampleInterval = value;
                        Assert.AreEqual(value, engine1.RuntimeHeapSizeSampleInterval);
                        Assert.AreEqual(value, engine2.RuntimeHeapSizeSampleInterval);
                        Assert.AreEqual(value, runtime.HeapSizeSampleInterval);

                        value = TimeSpan.FromMilliseconds(987654321.0);
                        runtime.HeapSizeSampleInterval = value;
                        Assert.AreEqual(value, engine1.RuntimeHeapSizeSampleInterval);
                        Assert.AreEqual(value, engine2.RuntimeHeapSizeSampleInterval);
                        Assert.AreEqual(value, runtime.HeapSizeSampleInterval);
                    }
                }
            }
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_MaxRuntimeStackUsage_Plumbing()
        {
            using (var runtime = new V8Runtime())
            {
                using (var engine1 = runtime.CreateScriptEngine())
                {
                    using (var engine2 = runtime.CreateScriptEngine())
                    {
                        var value = (UIntPtr)123456;
                        engine1.MaxRuntimeStackUsage = value;
                        Assert.AreEqual(value, engine1.MaxRuntimeStackUsage);
                        Assert.AreEqual(value, engine2.MaxRuntimeStackUsage);
                        Assert.AreEqual(value, runtime.MaxStackUsage);
                        Assert.AreEqual(UIntPtr.Zero, engine1.MaxRuntimeHeapSize);
                        Assert.AreEqual(UIntPtr.Zero, engine2.MaxRuntimeHeapSize);
                        Assert.AreEqual(UIntPtr.Zero, runtime.MaxHeapSize);

                        value = (UIntPtr)654321;
                        runtime.MaxStackUsage = value;
                        Assert.AreEqual(value, engine1.MaxRuntimeStackUsage);
                        Assert.AreEqual(value, engine2.MaxRuntimeStackUsage);
                        Assert.AreEqual(value, runtime.MaxStackUsage);
                        Assert.AreEqual(UIntPtr.Zero, engine1.MaxRuntimeHeapSize);
                        Assert.AreEqual(UIntPtr.Zero, engine2.MaxRuntimeHeapSize);
                        Assert.AreEqual(UIntPtr.Zero, runtime.MaxHeapSize);
                    }
                }
            }
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_MaxRuntimeStackUsage_ScriptOnly()
        {
            engine.MaxRuntimeStackUsage = (UIntPtr)(16 * 1024);
            TestUtil.AssertException<ScriptEngineException>(() => engine.Execute("(function () { arguments.callee(); })()"), false);
            Assert.AreEqual(Math.PI, engine.Evaluate("Math.PI"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_MaxRuntimeStackUsage_HostBounce()
        {
            engine.MaxRuntimeStackUsage = (UIntPtr)(16 * 1024);
            dynamic foo = engine.Evaluate("(function () { arguments.callee(); })");
            engine.Script.bar = new Action(() => foo());
            TestUtil.AssertException<ScriptEngineException>(() => engine.Execute("bar()"), false);
            Assert.AreEqual(Math.PI, engine.Evaluate("Math.PI"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_MaxRuntimeStackUsage_Alternating()
        {
            engine.MaxRuntimeStackUsage = (UIntPtr)(16 * 1024);
            dynamic foo = engine.Evaluate("(function () { bar(); })");
            engine.Script.bar = new Action(() => foo());
            TestUtil.AssertException<ScriptEngineException>(() => foo(), false);
            Assert.AreEqual(Math.PI, engine.Evaluate("Math.PI"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_MaxRuntimeStackUsage_Expansion()
        {
            engine.MaxRuntimeStackUsage = (UIntPtr)(16 * 1024);
            TestUtil.AssertException<ScriptEngineException>(() => engine.Execute("count = 0; (function () { count++; arguments.callee(); })()"), false);
            var count1 = engine.Script.count;
            engine.MaxRuntimeStackUsage = (UIntPtr)(64 * 1024);
            TestUtil.AssertException<ScriptEngineException>(() => engine.Execute("count = 0; (function () { count++; arguments.callee(); })()"), false);
            var count2 = engine.Script.count;
            Assert.IsTrue(count2 >= (count1 * 2));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_COMObject_FileSystemObject()
        {
            var list = new ArrayList();

            engine.Script.host = new ExtendedHostFunctions();
            engine.Script.list = list;
            engine.Execute(@"
                fso = host.newComObj('Scripting.FileSystemObject');
                drives = fso.Drives;
                e = drives.GetEnumerator();
                while (e.MoveNext()) {
                    list.Add(e.Current.Path);
                }
            ");

            var drives = DriveInfo.GetDrives();
            Assert.AreEqual(drives.Length, list.Count);
            Assert.IsTrue(drives.Select(drive => drive.Name.Substring(0, 2)).SequenceEqual(list.ToArray()));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_COMObject_Dictionary()
        {
            engine.Script.host = new ExtendedHostFunctions();
            engine.Execute(@"
                dict = host.newComObj('Scripting.Dictionary');
                dict.Add('foo', Math.PI);
                dict.Add('bar', Math.E);
                dict.Add('baz', 'abc');
            ");

            Assert.AreEqual(Math.PI, engine.Evaluate("dict.Item('foo')"));
            Assert.AreEqual(Math.PI, engine.Evaluate("dict.Item.get('foo')"));
            Assert.AreEqual(Math.E, engine.Evaluate("dict.Item('bar')"));
            Assert.AreEqual(Math.E, engine.Evaluate("dict.Item.get('bar')"));
            Assert.AreEqual("abc", engine.Evaluate("dict.Item('baz')"));
            Assert.AreEqual("abc", engine.Evaluate("dict.Item.get('baz')"));

            engine.Execute(@"
                dict.Item.set('foo', 'pushkin');
                dict.Item.set('bar', 'gogol');
                dict.Item.set('baz', Math.PI * Math.E);
            ");

            Assert.AreEqual("pushkin", engine.Evaluate("dict.Item('foo')"));
            Assert.AreEqual("pushkin", engine.Evaluate("dict.Item.get('foo')"));
            Assert.AreEqual("gogol", engine.Evaluate("dict.Item('bar')"));
            Assert.AreEqual("gogol", engine.Evaluate("dict.Item.get('bar')"));
            Assert.AreEqual(Math.PI * Math.E, engine.Evaluate("dict.Item('baz')"));
            Assert.AreEqual(Math.PI * Math.E, engine.Evaluate("dict.Item.get('baz')"));

            engine.Execute(@"
                dict.Key.set('foo', 'qux');
                dict.Key.set('bar', Math.PI);
                dict.Key.set('baz', Math.E);
            ");

            Assert.AreEqual("pushkin", engine.Evaluate("dict.Item('qux')"));
            Assert.AreEqual("pushkin", engine.Evaluate("dict.Item.get('qux')"));
            Assert.AreEqual("gogol", engine.Evaluate("dict.Item(Math.PI)"));
            Assert.AreEqual("gogol", engine.Evaluate("dict.Item.get(Math.PI)"));
            Assert.AreEqual(Math.PI * Math.E, engine.Evaluate("dict.Item(Math.E)"));
            Assert.AreEqual(Math.PI * Math.E, engine.Evaluate("dict.Item.get(Math.E)"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_COMType_FileSystemObject()
        {
            var list = new ArrayList();

            engine.Script.host = new ExtendedHostFunctions();
            engine.Script.list = list;
            engine.Execute(@"
                FSO = host.comType('Scripting.FileSystemObject');
                fso = host.newObj(FSO);
                drives = fso.Drives;
                e = drives.GetEnumerator();
                while (e.MoveNext()) {
                    list.Add(e.Current.Path);
                }
            ");

            var drives = DriveInfo.GetDrives();
            Assert.AreEqual(drives.Length, list.Count);
            Assert.IsTrue(drives.Select(drive => drive.Name.Substring(0, 2)).SequenceEqual(list.ToArray()));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_COMType_Dictionary()
        {
            engine.Script.host = new ExtendedHostFunctions();
            engine.Execute(@"
                Dict = host.comType('Scripting.Dictionary');
                dict = host.newObj(Dict);
                dict.Add('foo', Math.PI);
                dict.Add('bar', Math.E);
                dict.Add('baz', 'abc');
            ");

            Assert.AreEqual(Math.PI, engine.Evaluate("dict.Item('foo')"));
            Assert.AreEqual(Math.PI, engine.Evaluate("dict.Item.get('foo')"));
            Assert.AreEqual(Math.E, engine.Evaluate("dict.Item('bar')"));
            Assert.AreEqual(Math.E, engine.Evaluate("dict.Item.get('bar')"));
            Assert.AreEqual("abc", engine.Evaluate("dict.Item('baz')"));
            Assert.AreEqual("abc", engine.Evaluate("dict.Item.get('baz')"));

            engine.Execute(@"
                dict.Item.set('foo', 'pushkin');
                dict.Item.set('bar', 'gogol');
                dict.Item.set('baz', Math.PI * Math.E);
            ");

            Assert.AreEqual("pushkin", engine.Evaluate("dict.Item('foo')"));
            Assert.AreEqual("pushkin", engine.Evaluate("dict.Item.get('foo')"));
            Assert.AreEqual("gogol", engine.Evaluate("dict.Item('bar')"));
            Assert.AreEqual("gogol", engine.Evaluate("dict.Item.get('bar')"));
            Assert.AreEqual(Math.PI * Math.E, engine.Evaluate("dict.Item('baz')"));
            Assert.AreEqual(Math.PI * Math.E, engine.Evaluate("dict.Item.get('baz')"));

            engine.Execute(@"
                dict.Key.set('foo', 'qux');
                dict.Key.set('bar', Math.PI);
                dict.Key.set('baz', Math.E);
            ");

            Assert.AreEqual("pushkin", engine.Evaluate("dict.Item('qux')"));
            Assert.AreEqual("pushkin", engine.Evaluate("dict.Item.get('qux')"));
            Assert.AreEqual("gogol", engine.Evaluate("dict.Item(Math.PI)"));
            Assert.AreEqual("gogol", engine.Evaluate("dict.Item.get(Math.PI)"));
            Assert.AreEqual(Math.PI * Math.E, engine.Evaluate("dict.Item(Math.E)"));
            Assert.AreEqual(Math.PI * Math.E, engine.Evaluate("dict.Item.get(Math.E)"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddCOMObject_FileSystemObject()
        {
            var list = new ArrayList();

            engine.Script.list = list;
            engine.AddCOMObject("fso", "Scripting.FileSystemObject");
            engine.Execute(@"
                drives = fso.Drives;
                e = drives.GetEnumerator();
                while (e.MoveNext()) {
                    list.Add(e.Current.Path);
                }
            ");

            var drives = DriveInfo.GetDrives();
            Assert.AreEqual(drives.Length, list.Count);
            Assert.IsTrue(drives.Select(drive => drive.Name.Substring(0, 2)).SequenceEqual(list.ToArray()));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddCOMObject_Dictionary()
        {
            engine.AddCOMObject("dict", new Guid("{ee09b103-97e0-11cf-978f-00a02463e06f}"));
            engine.Execute(@"
                dict.Add('foo', Math.PI);
                dict.Add('bar', Math.E);
                dict.Add('baz', 'abc');
            ");

            Assert.AreEqual(Math.PI, engine.Evaluate("dict.Item('foo')"));
            Assert.AreEqual(Math.PI, engine.Evaluate("dict.Item.get('foo')"));
            Assert.AreEqual(Math.E, engine.Evaluate("dict.Item('bar')"));
            Assert.AreEqual(Math.E, engine.Evaluate("dict.Item.get('bar')"));
            Assert.AreEqual("abc", engine.Evaluate("dict.Item('baz')"));
            Assert.AreEqual("abc", engine.Evaluate("dict.Item.get('baz')"));

            engine.Execute(@"
                dict.Item.set('foo', 'pushkin');
                dict.Item.set('bar', 'gogol');
                dict.Item.set('baz', Math.PI * Math.E);
            ");

            Assert.AreEqual("pushkin", engine.Evaluate("dict.Item('foo')"));
            Assert.AreEqual("pushkin", engine.Evaluate("dict.Item.get('foo')"));
            Assert.AreEqual("gogol", engine.Evaluate("dict.Item('bar')"));
            Assert.AreEqual("gogol", engine.Evaluate("dict.Item.get('bar')"));
            Assert.AreEqual(Math.PI * Math.E, engine.Evaluate("dict.Item('baz')"));
            Assert.AreEqual(Math.PI * Math.E, engine.Evaluate("dict.Item.get('baz')"));

            engine.Execute(@"
                dict.Key.set('foo', 'qux');
                dict.Key.set('bar', Math.PI);
                dict.Key.set('baz', Math.E);
            ");

            Assert.AreEqual("pushkin", engine.Evaluate("dict.Item('qux')"));
            Assert.AreEqual("pushkin", engine.Evaluate("dict.Item.get('qux')"));
            Assert.AreEqual("gogol", engine.Evaluate("dict.Item(Math.PI)"));
            Assert.AreEqual("gogol", engine.Evaluate("dict.Item.get(Math.PI)"));
            Assert.AreEqual(Math.PI * Math.E, engine.Evaluate("dict.Item(Math.E)"));
            Assert.AreEqual(Math.PI * Math.E, engine.Evaluate("dict.Item.get(Math.E)"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddCOMType_FileSystemObject()
        {
            var list = new ArrayList();

            engine.Script.list = list;
            engine.AddCOMType("FSO", "Scripting.FileSystemObject");
            engine.Execute(@"
                fso = new FSO();
                drives = fso.Drives;
                e = drives.GetEnumerator();
                while (e.MoveNext()) {
                    list.Add(e.Current.Path);
                }
            ");

            var drives = DriveInfo.GetDrives();
            Assert.AreEqual(drives.Length, list.Count);
            Assert.IsTrue(drives.Select(drive => drive.Name.Substring(0, 2)).SequenceEqual(list.ToArray()));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddCOMType_Dictionary()
        {
            engine.AddCOMType("Dict", new Guid("{ee09b103-97e0-11cf-978f-00a02463e06f}"));
            engine.Execute(@"
                dict = new Dict();
                dict.Add('foo', Math.PI);
                dict.Add('bar', Math.E);
                dict.Add('baz', 'abc');
            ");

            Assert.AreEqual(Math.PI, engine.Evaluate("dict.Item('foo')"));
            Assert.AreEqual(Math.PI, engine.Evaluate("dict.Item.get('foo')"));
            Assert.AreEqual(Math.E, engine.Evaluate("dict.Item('bar')"));
            Assert.AreEqual(Math.E, engine.Evaluate("dict.Item.get('bar')"));
            Assert.AreEqual("abc", engine.Evaluate("dict.Item('baz')"));
            Assert.AreEqual("abc", engine.Evaluate("dict.Item.get('baz')"));

            engine.Execute(@"
                dict.Item.set('foo', 'pushkin');
                dict.Item.set('bar', 'gogol');
                dict.Item.set('baz', Math.PI * Math.E);
            ");

            Assert.AreEqual("pushkin", engine.Evaluate("dict.Item('foo')"));
            Assert.AreEqual("pushkin", engine.Evaluate("dict.Item.get('foo')"));
            Assert.AreEqual("gogol", engine.Evaluate("dict.Item('bar')"));
            Assert.AreEqual("gogol", engine.Evaluate("dict.Item.get('bar')"));
            Assert.AreEqual(Math.PI * Math.E, engine.Evaluate("dict.Item('baz')"));
            Assert.AreEqual(Math.PI * Math.E, engine.Evaluate("dict.Item.get('baz')"));

            engine.Execute(@"
                dict.Key.set('foo', 'qux');
                dict.Key.set('bar', Math.PI);
                dict.Key.set('baz', Math.E);
            ");

            Assert.AreEqual("pushkin", engine.Evaluate("dict.Item('qux')"));
            Assert.AreEqual("pushkin", engine.Evaluate("dict.Item.get('qux')"));
            Assert.AreEqual("gogol", engine.Evaluate("dict.Item(Math.PI)"));
            Assert.AreEqual("gogol", engine.Evaluate("dict.Item.get(Math.PI)"));
            Assert.AreEqual(Math.PI * Math.E, engine.Evaluate("dict.Item(Math.E)"));
            Assert.AreEqual(Math.PI * Math.E, engine.Evaluate("dict.Item.get(Math.E)"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_AddCOMType_XMLHTTP()
        {
            int status = 0;
            string data = null;

            var thread = new Thread(() =>
            {
                using (var testEngine = new V8ScriptEngine(V8ScriptEngineFlags.EnableDebugging))
                {
                    testEngine.Script.onComplete = new Action<int, string>((xhrStatus, xhrData) =>
                    {
                        status = xhrStatus;
                        data = xhrData;
                        Dispatcher.ExitAllFrames();
                    });

                    Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                    {
                        // ReSharper disable AccessToDisposedClosure

                        testEngine.AddCOMType("XMLHttpRequest", "MSXML2.XMLHTTP");
                        testEngine.Execute(@"
                            xhr = new XMLHttpRequest();
                            xhr.open('POST', 'http://httpbin.org/post', true);
                            xhr.onreadystatechange = function() {
                                if (xhr.readyState == 4) {
                                    onComplete(xhr.status, JSON.parse(xhr.responseText).data);
                                }
                            };
                            xhr.send('Hello, world!');
                        ");

                        // ReSharper restore AccessToDisposedClosure
                    }));

                    Dispatcher.Run();
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.AreEqual(200, status);
            Assert.AreEqual("Hello, world!", data);
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_EnableAutoHostVariables()
        {
            const string pre = "123";
            var value = "foo";
            const int post = 456;

            engine.Execute("function foo(a, x, b) { var y = x; x = a + 'bar' + b; return y; }");
            Assert.AreEqual("foo", engine.Script.foo(pre, ref value, post));
            Assert.AreEqual("foo", value);  // JavaScript doesn't support output parameters

            engine.EnableAutoHostVariables = true;
            engine.Execute("function foo(a, x, b) { var y = x.value; x.value = a + 'bar' + b; return y; }");
            Assert.AreEqual("foo", engine.Script.foo(pre, ref value, post));
            Assert.AreEqual("123bar456", value);
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_EnableAutoHostVariables_Delegate()
        {
            const string pre = "123";
            var value = "foo";
            const int post = 456;

            engine.Execute("function foo(a, x, b) { var y = x; x = a + 'bar' + b; return y; }");
            var del = DelegateFactory.CreateDelegate<TestDelegate>(engine, engine.Evaluate("foo"));
            Assert.AreEqual("foo", del(pre, ref value, post));
            Assert.AreEqual("foo", value);  // JavaScript doesn't support output parameters

            engine.EnableAutoHostVariables = true;
            engine.Execute("function foo(a, x, b) { var y = x.value; x.value = a + 'bar' + b; return y; }");
            del = DelegateFactory.CreateDelegate<TestDelegate>(engine, engine.Evaluate("foo"));
            Assert.AreEqual("foo", del(pre, ref value, post));
            Assert.AreEqual("123bar456", value);
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_ExceptionMarshaling()
        {
            Exception exception = new IOException("something awful happened");
            engine.AddRestrictedHostObject("exception", exception);

            engine.Script.foo = new Action(() => { throw exception; });

            engine.Execute(@"
                function bar() {
                    try {
                        foo();
                        return false;
                    }
                    catch (ex) {
                        return ex.hostException.GetBaseException() === exception;
                    }
                }
            ");

            Assert.IsTrue(Convert.ToBoolean(engine.Evaluate("bar()")));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_Current()
        {
            // ReSharper disable AccessToDisposedClosure

            using (var innerEngine = new V8ScriptEngine())
            {
                engine.Script.test = new Action(() =>
                {
                    innerEngine.Script.test = new Action(() => Assert.AreSame(innerEngine, ScriptEngine.Current));
                    Assert.AreSame(engine, ScriptEngine.Current);
                    innerEngine.Execute("test()");
                    innerEngine.Script.test();
                    Assert.AreSame(engine, ScriptEngine.Current);
                });

                Assert.IsNull(ScriptEngine.Current);
                engine.Execute("test()");
                engine.Script.test();
                Assert.IsNull(ScriptEngine.Current);
            }

            // ReSharper restore AccessToDisposedClosure
        }


        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEnging_array_date_json_serialization()
        {

            var foo=(dynamic)engine.Evaluate("function Foo(){};Foo.prototype.go=function(){ this.val=this.val||1; return this.val++;};f=new Foo();");
            
            var jObj =(dynamic)engine.Evaluate("x={a:1,a1:function(){},b:'2',c:[1,2,3],d:{a:1,b:'b'},e:[{a:1,b:3},{a:2,b:'2',c:new Date()}],f:new Date()};")
            ;
            
            Newtonsoft.Json.JsonSerializer ser = new JsonSerializer();
       

            var njObj = (JObject)JContainer.FromObject(jObj, ser);


            Assert.AreEqual((int)njObj.SelectToken("e.[0].a"), 1);

            var cwa = new ClassWithArrays()
                      {
                          Nums = new int[] { 1, 2, 3 },
                          Strs = new string[] { "a", "b", "c" },
                          DateTimes = new List<DateTime>()
                                      {
                                          new DateTime(2000, 1, 1),
                                          new DateTime(2010, 10, 10, 10, 10, 10, DateTimeKind.Utc)
                                      }
                      };

            var strcwa = JObject.FromObject(cwa).ToString();

            var jCwa = engine.Evaluate("x=" + strcwa + ";");

            var cwa2 =JObject.FromObject(jCwa).ToObject<ClassWithArrays>();

            Assert.AreEqual(cwa.Nums[2], cwa2.Nums[2]);
            Assert.AreEqual(cwa.DateTimes[1], cwa2.DateTimes[1]);

            

        }

        public class Steve
        {
            public string A
            {
                get;
                set;
            }
            public string B
            {
                get;
                set;
            }
            public string Go()
            {
                return B;
            }
        }

        public class Jon
        {
            public string Go()
            {
                return "food";
            }
        }

        public class Helper1
        {
            public string ClassToString(object obj)
            {
                return "the string is " + (obj ?? "ug").ToString();
            }
        }
        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DefineHostProperty()
        {
            var helper = new Helper1();
            var steve = new Steve();
            steve.B = "ok";
            var jon = new Jon();
            var jobj = JObject.Parse("{\"a\":2}");
            
            engine.AddHostObject("Helper1", helper);

            engine.AddHostObject("steve", steve);
            engine.Execute(@"
            Object.defineProperty(Helper1.constructor.prototype, 'toPOJO',
{
    value: function () {
        return Helper1.ClassToString(this);
    }
});
            ");
            engine.AddHostObject("jon", jon);
            var fn = (dynamic)engine.Evaluate("fn=function(x){ return x.toPOJO();}");
            var jonVal = fn.call(null,jon);
            var jsonVal = fn.call(null, jobj);
            var steveVal = fn.call(null, steve);
            Assert.AreEqual(helper.ClassToString(steve) ,(string)steveVal);
            Assert.AreEqual(helper.ClassToString(jon), (string)jonVal);
            Assert.AreEqual(helper.ClassToString(jobj), (string)jsonVal);

            


        }

        public static class Helper2
        {
            public  static string ClassToString(object obj)
            {
                return "the string is " + (obj ?? "ug").ToString();
            }
        }
       

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_EnableCaseInsensitivePropertyLookups()
        {
            engine.EnableCaseInsensitivePropertyLookups = true;
            var steve = new Steve();
            steve.B = "ok";

            var fn = (dynamic)engine.Evaluate("fn=function(x){ return x.b;}");
            var bVal = fn(steve);
            Assert.AreEqual((string)bVal, steve.B);

        }


        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_SerializeDelegates()
        {
            engine.EnableCaseInsensitivePropertyLookups = true;
            var steve = new Steve();
            steve.B = "ok";

            var fn = (dynamic)engine.Evaluate("fn=function(x){ return JSON.stringify(x); }");
            Func<string> go = steve.Go;

            var bVal = fn(go);
            Assert.AreEqual((string)bVal, "{}");

        }
        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_JSON_stringify()
        {
            //Map m = new Map();
            //m.Add("a",1);
            //engine.AddHostItem("map", HostItemFlags.None, m);
            //var mapString = (string)engine.Evaluate("JSON.stringify(map);");
            engine.AddHostType("xx", typeof(TestExtensions));

            var x = JArray.Parse("[\"a\",\"b\",1]");
            //var x = new List<int>() { 1, 2, 3 };
            engine.Execute("function foo(x){ return x.IsList()}");
            
            //engine.Execute("function foo(x){ var ret='';for(k in x){ret =ret+k;} return ret;}");
            //var ret =(string)engine.Script.foo(jarray);

            //var x = new Version(1, 2, 3, 4);
            var ret3 = engine.Script.foo(x);


            //List<object> list = new List<object>();
            //list.Add(1);
            //list.Add("b");

            //engine.AddHostType("sdf", typeof(TestExtensions));
            //engine.AddHostItem("list", HostItemFlags.None, list);

            //var listString = (string)engine.Evaluate("JSON.stringify(list);");
            //var listString2 = (string)engine.Evaluate("JSON.stringify([1,2,3,list]);");

        }
        public class ClassWithArrays
        {
            public int[] Nums { get; set; }
            public string[] Strs { get; set; }

            public List<DateTime> DateTimes { get; set; }
        }
        public class Map : DynamicObject, IEnumerable
        {
            private readonly IDictionary<string, object> dict = new Dictionary<string, object>();
            private IList list;
            public void Add(string key, object value)
            {
                dict.Add(key, value);
                list = null;
            }

            private List<string> _memberNames = null;
            public override IEnumerable<string> GetDynamicMemberNames()
            {
                if (_memberNames == null)
                {
                    _memberNames = new List<string>(dict.Keys.Count*2);
                    foreach (var name in dict.Keys)
                    {
                        _memberNames.Add(name);
                    }
                    foreach (var index in Enumerable.Range(0, dict.Count()))
                    {
                        _memberNames.Add(index.ToString(CultureInfo.InvariantCulture));
                    }
                }
                return _memberNames;
               
            }

            public override bool TrySetMember(SetMemberBinder binder, object value)
            {
                dict[binder.Name] = value;
                return true;
            }

            public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
            {
                dynamic obj;
                if (dict.TryGetValue(binder.Name, out obj))
                {
                    result = obj(args);
                }
                else
                {
                    result = null;
                }
                return true;
            }

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                var name = binder.Name;
                var found = dict.TryGetValue(name, out result);
                if (!found)
                {
                    int index;
                    if (int.TryParse(name, out index) && (index >= 0) && (index < dict.Count()))
                    {
                        if (list == null)
                        {
                            list = dict.ToList();
                        }
                        result = list[index];
                        found = true;
                    }
                }
                if (!found)
                {
                    //todo undefinde
                    result = null;
                }
                return true;
            }
            public IEnumerator GetEnumerator()
            {
                return dict.GetEnumerator();
            }
        }

        public class SimpleObject
        {
           
            public SimpleObject()
            {
                Bag = new PropertyBag();
                Bag.PropertyChanged += Bag_PropertyChanged;
            }

            private void Bag_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }

            public PropertyBag Bag { get; set; }

     
            public  string Go()
            {
                return "xxx";
            }
            public virtual decimal? Price { get; set; }

            public DateTime? Date { get; set; }
            public IV8ScriptItem V8Item { get; set; }

        }


        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngineEngineScope()
        {
            engine.Evaluate("function Foo(x){ this._x = x};Foo.prototype.setSO=function(x){ this._x=x;}; Foo.prototype.go=function (){ return this._x.Go()};Foo.prototype.add=function (k,v){ return this._x.Bag['a'+k]=v;};");
            var createFn = (dynamic)engine.Evaluate("a = function(x){ return new Foo( x);}");
            var so = new SimpleObject();
            var scope =engine.CreateHostItemScope();
            var foo = createFn(so);
            foo.add("a", 2);
            var go1 = foo.go();
            scope.Dispose();
            foo = createFn(so);
            var go3 = foo.go();


            foo = createFn();
            so = new SimpleObject();
            scope = engine.CreateHostItemScope();
            foo.setSO(so);
            scope.Dispose();

            try
            {
                var go2 = foo.go();
            }
            catch (ScriptEngineException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            try {
                var go2 = foo.add("b", 2);
            }
            catch (ScriptEngineException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            engine.Dispose();
          
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_SetNullDecimal()
        {
            var so = new SimpleObject();

            engine.AddHostObject("simple", so);

            engine.Evaluate("simple.Price=1.2");

            Assert.AreEqual(so.Price, (decimal)1.2);

        }

        public class ContainsListObject
        {
            public List<SimpleObject> List { get; set; }
            public List<decimal> DecimalList { get; set; }
            public List<decimal?> NullableDecimalList { get; set; }
            public List<string> StringList { get; set; }
        }


        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_SetHostList()
        {
            var obj = new ContainsListObject() { List = new List<SimpleObject>() };
            for ( var i =0; i < 10; i ++)
            {
                obj.List.Add(new SimpleObject() { Price = i + 1 });
            }
            engine.AddHostObject("clo", obj);
            engine.Evaluate("clo.constructor.prototype.length=" + obj.List.Count);
            var len1 = (int)engine.Evaluate("x=clo.List.length");
            engine.Evaluate("var arr= Array.prototype.filter.call( clo.List, function (a){ return a.Price>4;})");
            var arr =(dynamic) engine.Evaluate("arr= arr");
            var len = arr.length;
            engine.Evaluate("clo.List = arr;");
            Assert.AreEqual(obj.List.Count, 6);




        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_SetHostListOfValueTypes()
        {
            var clo = new ContainsListObject() { List = new List<SimpleObject>() };
            engine.AddHostObject("clo", clo);
            engine.Evaluate("clo.DecimalList=[1.2,3.4]");
            Assert.AreEqual(clo.DecimalList.First(), (decimal)1.2);
            engine.Evaluate("clo.DecimalList=[1,3,5.7,8]");
            Assert.AreEqual(clo.DecimalList[3], (decimal)8);
            engine.Evaluate("clo.NullableDecimalList=[1,3,null,,8]");
            Assert.AreEqual(clo.NullableDecimalList[3], null);


            

            engine.Evaluate("clo.StringList=['asd',,'ghi']");
            Assert.AreEqual(clo.StringList[2], "ghi");

            engine.Evaluate("clo.StringList=['asd','def','ghi']");
            Assert.AreEqual(clo.StringList[2], "ghi");


        }

        public class SimpleObjectContainer
        {
            public SimpleObject SO { get; set; }
        }


        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_ManagedTypeMapper()
        {
            typeof(SimpleObject).AddScriptItemMapper((x,t) =>
           {

               var map = x.GetPropertyMap();
              
               var bag = map.Where( entry => entry.Key .Equals("Bag", StringComparison.OrdinalIgnoreCase)).Select(entry=>entry.Value ).FirstOrDefault();
               //var bag = x.GetProperty("Bag");
               if ( bag != null && !(bag is IPropertyBag))
               {
                   bag = null;
               }
               var price = map.Where(entry => entry.Key.Equals("Price", StringComparison.OrdinalIgnoreCase)).Select(entry => entry.Value).FirstOrDefault();
             //  var price = names.Where(name => name.Equals("Price", StringComparison.OrdinalIgnoreCase)).Select(x.GetProperty).FirstOrDefault();
               if ( price != null && price.GetType().IsNumeric())
               {
                   price = Convert.ToDecimal(price);
               }

               var date = map.Where(entry => entry.Key.Equals("date", StringComparison.OrdinalIgnoreCase)).Select(entry => entry.Value).FirstOrDefault();
               //  var price = names.Where(name => name.Equals("Price", StringComparison.OrdinalIgnoreCase)).Select(x.GetProperty).FirstOrDefault();
               if (price != null )
               {
                   var priceMap = 
                   price = Convert.ToDecimal(price);
               }
               return new SimpleObject()
               {
                   Bag = bag as PropertyBag,
                   Price = price as decimal?
               };
           });

            

            var prodColString = "{\"facets\":[{\"label\":\"Active / Passive\",\"facetType\":\"Value\",\"field\":\"Tenant~Active-Passive\",\"values\":[{\"label\":\"Passive\",\"isApplied\":false,\"count\":1,\"value\":\"passive\",\"filterValue\":\"Tenant~Active-Passive:passive\",\"isDisplayed\":true,\"childrenFacetValues\":[]}]},{\"label\":\"Use\",\"facetType\":\"Value\",\"field\":\"tenant~Use\",\"values\":[{\"label\":\"Live Sound\",\"isApplied\":true,\"count\":1,\"value\":\"live_sound\",\"filterValue\":\"tenant~Use:live_sound\",\"isDisplayed\":true,\"childrenFacetValues\":[]}]},{\"label\":\"Speaker Size\",\"facetType\":\"Value\",\"field\":\"Tenant~Speaker-Size\",\"values\":[{\"label\":\"18\\\"\",\"isApplied\":false,\"count\":4,\"value\":\"18\",\"filterValue\":\"Tenant~Speaker-Size:18\",\"isDisplayed\":true,\"childrenFacetValues\":[]},{\"label\":\"15\\\"\",\"isApplied\":true,\"count\":1,\"value\":\"15\",\"filterValue\":\"Tenant~Speaker-Size:15\",\"isDisplayed\":true,\"childrenFacetValues\":[]}]},{\"label\":\"Price\",\"facetType\":\"RangeQuery\",\"field\":\"Price\",\"values\":[{\"label\":\"250 to 500\",\"isApplied\":false,\"count\":2,\"value\":\"price:[250 TO 500]\",\"filterValue\":\"price:[250 TO 500]\",\"rangeQueryValueStart\":\"250\",\"rangeQueryValueEnd\":\"500\",\"isDisplayed\":true,\"childrenFacetValues\":[]},{\"label\":\"150 and Under\",\"isApplied\":false,\"count\":1,\"value\":\"price:[* TO 150]\",\"filterValue\":\"price:[* TO 150]\",\"rangeQueryValueStart\":\"*\",\"rangeQueryValueEnd\":\"150\",\"isDisplayed\":true,\"childrenFacetValues\":[]},{\"label\":\"150 to 250\",\"isApplied\":true,\"count\":1,\"value\":\"price:[150 TO 250]\",\"filterValue\":\"price:[150 TO 250]\",\"rangeQueryValueStart\":\"150\",\"rangeQueryValueEnd\":\"250\",\"isDisplayed\":true,\"childrenFacetValues\":[]}]},{\"label\":\"Subwoofer Sets\",\"facetType\":\"Value\",\"field\":\"tenant~Subwoofer_Sets\",\"values\":[{\"label\":\"Single\",\"isApplied\":false,\"count\":1,\"value\":\"single\",\"filterValue\":\"tenant~Subwoofer_Sets:single\",\"isDisplayed\":true,\"childrenFacetValues\":[]}]}],\"startIndex\":0,\"pageSize\":15,\"pageCount\":1,\"totalCount\":1,\"items\":[{\"productCode\":\"Baby-Tremor\",\"productSequence\":88,\"productUsage\":\"Standard\",\"fulfillmentTypesSupported\":[\"DirectShip\",\"InStorePickup\"],\"goodsType\":\"Physical\",\"content\":{\"productName\":\"Baby Tremor 15\u2122 - 15\\\" Pro Audio Subwoofer Cabinet\",\"productFullDescription\":\"<div><b>Overview</b><br><br>When it comes to power and performance, there is nothing small about the Baby Tremor 15\\\" Subwoofer Cabinet! This high performance compact 15\\\" subwoofer is the ultimate in power, performance and portability for any PA setting. The Baby Tremor was designed with medium sized live sound and DJ setting in mind...compact size with deep bass production, this 15\\\" subwoofer produces all the deep low end bass you crave! <br><br><b>Features</b><br><br>It is constructed entirely of 7 ply Birch plywood and has a full metal grill. The cabinet houses a 15 \\\" Woofer that operates at 300 Watts RMS and 600 Watts Peak Power and reproduces low frequencies down to 35 Hz.  This 15\\\" Subwoofer is the perfect solution to any PA application, whether it be Clubs, DJ, partys, weddings, or Church events. The Baby Tremor 15 Inch Subwoofer also contains a pole mount to accommodate an additional speaker for optimal full range PA performance.  <br><br><b>The Seismic Audio Experience</b><br><br>The Seismic Audio brand was designed with the notion that every musician should have access to high quality Pro Audio gear at an affordable price! With your purchase, you will receive one 15 Inch Subwoofer pictured and described above.  You will also receive top notch customer service, plus all the technical expertise you will ever need is just a phone call or email away! Join the Seismic Audio revolution and see what tens of thousands already know.  Whether it is for the garage band, live sound, stage, or studio...Seismic Audio is bang for the buck, the best Pro Audio gear for all PA applications! Need a Pair of 15\\\" Subwoofers? Buy them here!</div>\",\"productShortDescription\":\"Model #: Baby Tremor\u2122 (15\\\" Sub Woofer)\\n15\\\" PA Subwoofer Loudspeaker\\n300 Watts RMS / 8 Ohm\",\"metaTagTitle\":\"15 Inch Subwoofer Bass Cabinet - 300 Watts RMS | 15 Inch Sub Cab | 15 Inch sub woofer | 15 sub | Passive 15 Inch Sub woofer\",\"metaTagDescription\":\"Seismic Audio offers the highest quality Passive PA Subwoofers at the lowest cost with the best selection on the web. Buy a 15 Inch Subwoofer Bass Cabinet -the sub cab is rated at 300 watts RMS.  Get that low end you crave.  Seismic Audio offers Free Shipping on Unpowered PA Subwoofers and orders ship the same day when placed by 2:30 CST.\",\"metaTagKeywords\":\"15\\\" subwoofer cabinet, 15\\\" subwoofer cabinets, 15 subwoofer cabinet, 15 subwoofer cabinets, 15\\\" sub woofer cabinet, 15\\\" sub woofer cabinets, 15 sub woofer cabinet, 15 sub woofer cabinets, 15\\\" subwoofer cab, 15\\\" subwoofer cab, 15 subwoofer cab, 15 subwoofer cabs, 15\\\" sub woofer cab, 15\\\" sub woofer cabs, 15 sub woofer cab, 15 sub woofer cabs, 15 inch sub, 15\\\" sub, 15\\\" subs\",\"seoFriendlyUrl\":\"15-inch-subwoofer-cabinet\",\"productImages\":[{\"altText\":\"\",\"imageUrl\":\"//cdn-tp1.mozu.com/2199-2317/cms/2317/files/b7d36f5f-064b-459c-a9a4-72a2cd371203\",\"cmsId\":\"b7d36f5f-064b-459c-a9a4-72a2cd371203\",\"sequence\":1},{\"altText\":\"\",\"imageUrl\":\"//cdn-tp1.mozu.com/2199-2317/cms/2317/files/f85e2acf-00b5-4fce-bc8c-1faac01b614f\",\"cmsId\":\"f85e2acf-00b5-4fce-bc8c-1faac01b614f\",\"sequence\":2},{\"altText\":\"\",\"imageUrl\":\"//cdn-tp1.mozu.com/2199-2317/cms/2317/files/b863a98f-b2b8-49bf-8733-68ae498f1f64\",\"cmsId\":\"b863a98f-b2b8-49bf-8733-68ae498f1f64\",\"sequence\":3},{\"altText\":\"\",\"imageUrl\":\"//cdn-tp1.mozu.com/2199-2317/cms/2317/files/a874982f-a65a-47f7-8b23-734d0aee63b3\",\"cmsId\":\"a874982f-a65a-47f7-8b23-734d0aee63b3\",\"sequence\":4},{\"altText\":\"\",\"imageUrl\":\"//cdn-tp1.mozu.com/2199-2317/cms/2317/files/66f8436a-000b-4b5a-a78a-757a1564f93a\",\"cmsId\":\"66f8436a-000b-4b5a-a78a-757a1564f93a\",\"sequence\":5},{\"altText\":\"\",\"imageUrl\":\"//cdn-tp1.mozu.com/2199-2317/cms/2317/files/51ac4493-08f0-4080-a646-a286713bfd04\",\"cmsId\":\"51ac4493-08f0-4080-a646-a286713bfd04\",\"sequence\":6}]},\"purchasableState\":{\"isPurchasable\":true},\"isActive\":true,\"publishState\":\"Live\",\"price\":{\"msrp\":359.9900,\"price\":199.9900,\"priceType\":\"List\",\"catalogListPrice\":199.9900},\"productType\":\"PA Subwoofers\",\"productTypeId\":57,\"isTaxable\":true,\"pricingBehavior\":{\"discountsRestricted\":false},\"inventoryInfo\":{\"manageStock\":true,\"outOfStockBehavior\":\"DisplayMessage\",\"onlineStockAvailable\":27,\"onlineSoftStockAvailable\":27,\"onlineLocationCode\":\"Warehouse-01\"},\"createDate\":\"2014-03-18T05:01:08.956Z\",\"dateFirstAvailableInCatalog\":\"2014-03-18T05:01:09.003Z\",\"daysAvailableInCatalog\":636,\"upc\":\"847861012779\",\"upCs\":[\"847861012779\"],\"mfgPartNumber\":\"\",\"mfgPartNumbers\":[\"\"],\"categories\":[{\"categoryId\":423,\"parentCategory\":{\"categoryId\":421,\"content\":{\"categoryImages\":[],\"name\":\"Sub Navigation\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"\",\"metaTagDescription\":\"\",\"metaTagKeywords\":\"\",\"slug\":\"shop-by-dept\"},\"childrenCategories\":[],\"sequence\":1,\"isDisplayed\":true,\"categoryCode\":\"423\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"Church Gear\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"Church Gear - Seismic Audio Speakers\",\"metaTagDescription\":\"Buy Pro Church Gear at Seismic Audio Speakers Today! Choose from Church Speakers, Chruch Subwoofers, Church Monitors, Church Rack Cases & More.\",\"metaTagKeywords\":\"audio equipment, church gear, church speakers, church subwoofers, church monitors, church rack cases, church cables, seismic audio speakers\",\"slug\":\"Church-Gear\"},\"childrenCategories\":[],\"sequence\":1,\"isDisplayed\":true,\"categoryCode\":\"423\",\"count\":0},{\"categoryId\":424,\"parentCategory\":{\"categoryId\":421,\"content\":{\"categoryImages\":[],\"name\":\"Sub Navigation\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"\",\"metaTagDescription\":\"\",\"metaTagKeywords\":\"\",\"slug\":\"shop-by-dept\"},\"childrenCategories\":[],\"sequence\":3,\"isDisplayed\":true,\"categoryCode\":\"424\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"DJ Gear\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"DJ Gear - Seismic Audio Speakers\",\"metaTagDescription\":\"Shop our DJ Gear. We have all the DJ Gear you need to Improve your sound!  Rock every party with Powered DJ Speakers, factory direct DJ Speaker Packages available. Free Shipping!\",\"metaTagKeywords\":\"Powered, DJ, speakers, packages, cabinets, molded, loud, monitors, pro audio, factory direct, quality, low price.\",\"slug\":\"dj-gear\"},\"childrenCategories\":[],\"sequence\":3,\"isDisplayed\":true,\"categoryCode\":\"424\",\"count\":0},{\"categoryId\":426,\"parentCategory\":{\"categoryId\":423,\"parentCategory\":{\"categoryId\":421,\"content\":{\"categoryImages\":[],\"name\":\"Sub Navigation\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"\",\"metaTagDescription\":\"\",\"metaTagKeywords\":\"\",\"slug\":\"shop-by-dept\"},\"childrenCategories\":[],\"sequence\":2,\"isDisplayed\":true,\"categoryCode\":\"426\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"Church Gear\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"Church Gear - Seismic Audio Speakers\",\"metaTagDescription\":\"Buy Pro Church Gear at Seismic Audio Speakers Today! Choose from Church Speakers, Chruch Subwoofers, Church Monitors, Church Rack Cases & More.\",\"metaTagKeywords\":\"audio equipment, church gear, church speakers, church subwoofers, church monitors, church rack cases, church cables, seismic audio speakers\",\"slug\":\"Church-Gear\"},\"childrenCategories\":[],\"sequence\":2,\"isDisplayed\":true,\"categoryCode\":\"426\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"Church Subwoofers\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"Church Subwoofers - Seismic Audio Speakers\",\"metaTagDescription\":\"Add a subwoofer to your church's PA System for even better sound quality. Choose from 15\\\" subwoofers, 18\\\" subwoofers and powered subwoofers all with church-friendly price tags.\",\"metaTagKeywords\":\"church speakers, church pa system, church audio equipment, church subwoofer, powered church subwoofer, cheap church speakers\",\"slug\":\"Church-Subwoofers\"},\"childrenCategories\":[],\"sequence\":2,\"isDisplayed\":true,\"categoryCode\":\"426\",\"count\":0},{\"categoryId\":434,\"parentCategory\":{\"categoryId\":424,\"parentCategory\":{\"categoryId\":421,\"content\":{\"categoryImages\":[],\"name\":\"Sub Navigation\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"\",\"metaTagDescription\":\"\",\"metaTagKeywords\":\"\",\"slug\":\"shop-by-dept\"},\"childrenCategories\":[],\"sequence\":3,\"isDisplayed\":true,\"categoryCode\":\"434\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"DJ Gear\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"DJ Gear - Seismic Audio Speakers\",\"metaTagDescription\":\"Shop our DJ Gear. We have all the DJ Gear you need to Improve your sound!  Rock every party with Powered DJ Speakers, factory direct DJ Speaker Packages available. Free Shipping!\",\"metaTagKeywords\":\"Powered, DJ, speakers, packages, cabinets, molded, loud, monitors, pro audio, factory direct, quality, low price.\",\"slug\":\"dj-gear\"},\"childrenCategories\":[],\"sequence\":3,\"isDisplayed\":true,\"categoryCode\":\"434\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"DJ Subwoofers\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"DJ Subwoofers | Seismic Audio Speakers\",\"metaTagDescription\":\"Shop DJ Subwoofers at Seismic Audio Speakers. 15 inch Dj Subwoofers, 18 inch DJ Subwoofers, Powered DJ Subwoofers, and Active DJ Subwoofers. Order now and get Free shipping!\",\"metaTagKeywords\":\"dj subwoofers, 18\\\" dj subwoofers, 15\\\" dj subwoofers, powered dj subwoofers, dj speakers, dj gear, seismic audio speakers\",\"slug\":\"dj-subwoofers\"},\"childrenCategories\":[],\"sequence\":3,\"isDisplayed\":true,\"categoryCode\":\"434\",\"count\":0},{\"categoryId\":471,\"parentCategory\":{\"categoryId\":434,\"parentCategory\":{\"categoryId\":424,\"parentCategory\":{\"categoryId\":421,\"content\":{\"categoryImages\":[],\"name\":\"Sub Navigation\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"\",\"metaTagDescription\":\"\",\"metaTagKeywords\":\"\",\"slug\":\"shop-by-dept\"},\"childrenCategories\":[],\"sequence\":3,\"isDisplayed\":true,\"categoryCode\":\"471\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"DJ Gear\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"DJ Gear - Seismic Audio Speakers\",\"metaTagDescription\":\"Shop our DJ Gear. We have all the DJ Gear you need to Improve your sound!  Rock every party with Powered DJ Speakers, factory direct DJ Speaker Packages available. Free Shipping!\",\"metaTagKeywords\":\"Powered, DJ, speakers, packages, cabinets, molded, loud, monitors, pro audio, factory direct, quality, low price.\",\"slug\":\"dj-gear\"},\"childrenCategories\":[],\"sequence\":3,\"isDisplayed\":true,\"categoryCode\":\"471\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"DJ Subwoofers\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"DJ Subwoofers | Seismic Audio Speakers\",\"metaTagDescription\":\"Shop DJ Subwoofers at Seismic Audio Speakers. 15 inch Dj Subwoofers, 18 inch DJ Subwoofers, Powered DJ Subwoofers, and Active DJ Subwoofers. Order now and get Free shipping!\",\"metaTagKeywords\":\"dj subwoofers, 18\\\" dj subwoofers, 15\\\" dj subwoofers, powered dj subwoofers, dj speakers, dj gear, seismic audio speakers\",\"slug\":\"dj-subwoofers\"},\"childrenCategories\":[],\"sequence\":3,\"isDisplayed\":true,\"categoryCode\":\"471\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"15\\\" DJ Subwoofers\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"15 Inch DJ Subwoofer Cabinets | Active 15 DJ Subwoofers | Seismic Audio\",\"metaTagDescription\":\"Seismic Audio 15 Inch DJ Subwoofer cabinets provide the deep bass to make your PA complete! Shop our 15 Inch DJ Subwoofers and get free shipping!\",\"metaTagKeywords\":\"\",\"slug\":\"15-dj-subwoofers\"},\"childrenCategories\":[],\"sequence\":3,\"isDisplayed\":true,\"categoryCode\":\"471\",\"count\":0},{\"categoryId\":827,\"parentCategory\":{\"categoryId\":422,\"content\":{\"categoryImages\":[],\"name\":\"Alternate Navigation\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"\",\"metaTagDescription\":\"\",\"metaTagKeywords\":\"\",\"slug\":\"alternate-navigation\"},\"childrenCategories\":[],\"sequence\":1,\"isDisplayed\":true,\"categoryCode\":\"827\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"Subwoofers\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"Pro Audio Subwoofers | PA Subwoofers | PA Subs | Stage Subwoofers | Seismic Audio Speakers\",\"metaTagDescription\":\"Learn more about Pro Audio Subwoofers at Seismic Audio. Take advantage of the lowest prices and best selection of PA Subwoofers on the web. We offer Active PA Subwoofers, Passive PA Subwoofers, Powered PA Subwoofers, Unpowered PA Subwoofers,  Studio Subwoofers, as well as replacement PA Subwoofers. Seismic Audio offers Free Shipping and orders ship the same day when placed by 2:30 CST.\",\"metaTagKeywords\":\"Subwoofer, powered, sub, cabinet, 18 inch, 15 inch, pro audio, quality, loudspeaker, PA, cabinet. sub, woofer, 18\\\" subwoofer, 15\\\" Subwoofer, studio Subwoofer, 18\\\" sub woofer, 15\\\" Sub woofer, studio Sub woofer,  18 inch subwoofer, 15 inch Subwoofer, studio Subwoofer, 18\\\" subwoofers, 15\\\" Subwoofers, studio Subwoofers,\",\"slug\":\"subwoofers\"},\"childrenCategories\":[],\"sequence\":1,\"isDisplayed\":true,\"categoryCode\":\"827\",\"count\":0},{\"categoryId\":832,\"parentCategory\":{\"categoryId\":827,\"parentCategory\":{\"categoryId\":422,\"content\":{\"categoryImages\":[],\"name\":\"Alternate Navigation\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"\",\"metaTagDescription\":\"\",\"metaTagKeywords\":\"\",\"slug\":\"alternate-navigation\"},\"childrenCategories\":[],\"sequence\":4,\"isDisplayed\":true,\"categoryCode\":\"832\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"Subwoofers\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"Pro Audio Subwoofers | PA Subwoofers | PA Subs | Stage Subwoofers | Seismic Audio Speakers\",\"metaTagDescription\":\"Learn more about Pro Audio Subwoofers at Seismic Audio. Take advantage of the lowest prices and best selection of PA Subwoofers on the web. We offer Active PA Subwoofers, Passive PA Subwoofers, Powered PA Subwoofers, Unpowered PA Subwoofers,  Studio Subwoofers, as well as replacement PA Subwoofers. Seismic Audio offers Free Shipping and orders ship the same day when placed by 2:30 CST.\",\"metaTagKeywords\":\"Subwoofer, powered, sub, cabinet, 18 inch, 15 inch, pro audio, quality, loudspeaker, PA, cabinet. sub, woofer, 18\\\" subwoofer, 15\\\" Subwoofer, studio Subwoofer, 18\\\" sub woofer, 15\\\" Sub woofer, studio Sub woofer,  18 inch subwoofer, 15 inch Subwoofer, studio Subwoofer, 18\\\" subwoofers, 15\\\" Subwoofers, studio Subwoofers,\",\"slug\":\"subwoofers\"},\"childrenCategories\":[],\"sequence\":4,\"isDisplayed\":true,\"categoryCode\":\"832\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"15\\\" Subwoofers\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"15 Inch Subwoofer Cabinets | 15 Inch Sub Cabs | 15 Inch sub woofers | 15 Inch Sub Woofer Cabinets\",\"metaTagDescription\":\"Order Seismic Audio 15 inch PA Subwoofers at the lowest cost with the highest quality on the web.  For bands or DJ's our 18 Inch Subwoofers give that low end you crave, plus free shipping.\",\"metaTagKeywords\":\"\",\"slug\":\"15-subwoofers\"},\"childrenCategories\":[],\"sequence\":4,\"isDisplayed\":true,\"categoryCode\":\"832\",\"count\":0},{\"categoryId\":885,\"parentCategory\":{\"categoryId\":421,\"content\":{\"categoryImages\":[],\"name\":\"Sub Navigation\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"\",\"metaTagDescription\":\"\",\"metaTagKeywords\":\"\",\"slug\":\"shop-by-dept\"},\"childrenCategories\":[],\"sequence\":0,\"isDisplayed\":true,\"categoryCode\":\"885\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"Live Sound Gear\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"Live Sound Gear | Seismic Audio Speakers\",\"metaTagDescription\":\"Shop our Live Sound Gear. We have all the Live Sound Gear you need to Improve your sound!  Rock every live event  with Powered live sound Speakers, factory direct live sound Speaker Packages available. Free Shipping!\",\"metaTagKeywords\":\"Powered, live sound, speakers, packages, cabinets, molded, loud, monitors, pro audio, factory direct, quality, low price.\",\"slug\":\"live-sound-gear\"},\"childrenCategories\":[],\"sequence\":0,\"isDisplayed\":true,\"categoryCode\":\"885\",\"count\":0},{\"categoryId\":886,\"parentCategory\":{\"categoryId\":885,\"parentCategory\":{\"categoryId\":421,\"content\":{\"categoryImages\":[],\"name\":\"Sub Navigation\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"\",\"metaTagDescription\":\"\",\"metaTagKeywords\":\"\",\"slug\":\"shop-by-dept\"},\"childrenCategories\":[],\"sequence\":1,\"isDisplayed\":true,\"categoryCode\":\"886\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"Live Sound Gear\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"Live Sound Gear | Seismic Audio Speakers\",\"metaTagDescription\":\"Shop our Live Sound Gear. We have all the Live Sound Gear you need to Improve your sound!  Rock every live event  with Powered live sound Speakers, factory direct live sound Speaker Packages available. Free Shipping!\",\"metaTagKeywords\":\"Powered, live sound, speakers, packages, cabinets, molded, loud, monitors, pro audio, factory direct, quality, low price.\",\"slug\":\"live-sound-gear\"},\"childrenCategories\":[],\"sequence\":1,\"isDisplayed\":true,\"categoryCode\":\"886\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"Live Sound\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"Live Sound | Seismic Audio\",\"metaTagDescription\":\"Shop our selection of live sound speakers, loudspeakers, subwoofers, monitors and all of your pro audio live sound needs! Order live sound speakers and get free shipping from Seismic Audio!\",\"metaTagKeywords\":\"\",\"slug\":\"live-sound\"},\"childrenCategories\":[],\"sequence\":1,\"isDisplayed\":true,\"categoryCode\":\"886\",\"count\":0},{\"categoryId\":889,\"parentCategory\":{\"categoryId\":886,\"parentCategory\":{\"categoryId\":885,\"parentCategory\":{\"categoryId\":421,\"content\":{\"categoryImages\":[],\"name\":\"Sub Navigation\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"\",\"metaTagDescription\":\"\",\"metaTagKeywords\":\"\",\"slug\":\"shop-by-dept\"},\"childrenCategories\":[],\"sequence\":4,\"isDisplayed\":true,\"categoryCode\":\"889\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"Live Sound Gear\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"Live Sound Gear | Seismic Audio Speakers\",\"metaTagDescription\":\"Shop our Live Sound Gear. We have all the Live Sound Gear you need to Improve your sound!  Rock every live event  with Powered live sound Speakers, factory direct live sound Speaker Packages available. Free Shipping!\",\"metaTagKeywords\":\"Powered, live sound, speakers, packages, cabinets, molded, loud, monitors, pro audio, factory direct, quality, low price.\",\"slug\":\"live-sound-gear\"},\"childrenCategories\":[],\"sequence\":4,\"isDisplayed\":true,\"categoryCode\":\"889\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"Live Sound\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"Live Sound | Seismic Audio\",\"metaTagDescription\":\"Shop our selection of live sound speakers, loudspeakers, subwoofers, monitors and all of your pro audio live sound needs! Order live sound speakers and get free shipping from Seismic Audio!\",\"metaTagKeywords\":\"\",\"slug\":\"live-sound\"},\"childrenCategories\":[],\"sequence\":4,\"isDisplayed\":true,\"categoryCode\":\"889\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"Stage Subwoofers\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"Stage Subwoofers | Seismic Audio\",\"metaTagDescription\":\"Shop all of your subwoofer needs for your next live show.  We have powered and passive subwoofers in various sizes.  Choose Seismic Audio and get free shipping on stage subwoofers!\",\"metaTagKeywords\":\"\",\"slug\":\"stage-subwoofers\"},\"childrenCategories\":[],\"sequence\":4,\"isDisplayed\":true,\"categoryCode\":\"889\",\"count\":0},{\"categoryId\":928,\"content\":{\"categoryImages\":[],\"name\":\"Master Category\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"\",\"metaTagDescription\":\"\",\"metaTagKeywords\":\"\",\"slug\":\"master-category\"},\"childrenCategories\":[],\"sequence\":2,\"categoryCode\":\"928\",\"count\":0},{\"categoryId\":1038,\"parentCategory\":{\"categoryId\":827,\"parentCategory\":{\"categoryId\":422,\"content\":{\"categoryImages\":[],\"name\":\"Alternate Navigation\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"\",\"metaTagDescription\":\"\",\"metaTagKeywords\":\"\",\"slug\":\"alternate-navigation\"},\"childrenCategories\":[],\"sequence\":9,\"isDisplayed\":true,\"categoryCode\":\"1038\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"Subwoofers\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"Pro Audio Subwoofers | PA Subwoofers | PA Subs | Stage Subwoofers | Seismic Audio Speakers\",\"metaTagDescription\":\"Learn more about Pro Audio Subwoofers at Seismic Audio. Take advantage of the lowest prices and best selection of PA Subwoofers on the web. We offer Active PA Subwoofers, Passive PA Subwoofers, Powered PA Subwoofers, Unpowered PA Subwoofers,  Studio Subwoofers, as well as replacement PA Subwoofers. Seismic Audio offers Free Shipping and orders ship the same day when placed by 2:30 CST.\",\"metaTagKeywords\":\"Subwoofer, powered, sub, cabinet, 18 inch, 15 inch, pro audio, quality, loudspeaker, PA, cabinet. sub, woofer, 18\\\" subwoofer, 15\\\" Subwoofer, studio Subwoofer, 18\\\" sub woofer, 15\\\" Sub woofer, studio Sub woofer,  18 inch subwoofer, 15 inch Subwoofer, studio Subwoofer, 18\\\" subwoofers, 15\\\" Subwoofers, studio Subwoofers,\",\"slug\":\"subwoofers\"},\"childrenCategories\":[],\"sequence\":9,\"isDisplayed\":true,\"categoryCode\":\"1038\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"Tremor Series PA Subwoofers\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"Tremor Series PA Subwoofers | Subwoofer Cabinets | Seismic Audio\",\"metaTagDescription\":\"Shop our Tremor Series of PA Subwoofers and DJ Subwoofers.  The Tremor series offer 18 inch subwoofers, 15 inch subwoofers, 12 Inch subwoofers, and 10 inch subwoofers. Active and Passive models available.  Order PA Subwoofers and get free shipping from Seismic Audio.\",\"metaTagKeywords\":\"\",\"slug\":\"tremor-series-pa-subwoofers\"},\"childrenCategories\":[],\"sequence\":9,\"isDisplayed\":true,\"categoryCode\":\"1038\",\"count\":0},{\"categoryId\":1053,\"parentCategory\":{\"categoryId\":827,\"parentCategory\":{\"categoryId\":422,\"content\":{\"categoryImages\":[],\"name\":\"Alternate Navigation\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"\",\"metaTagDescription\":\"\",\"metaTagKeywords\":\"\",\"slug\":\"alternate-navigation\"},\"childrenCategories\":[],\"sequence\":2,\"isDisplayed\":true,\"categoryCode\":\"1053\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"Subwoofers\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"Pro Audio Subwoofers | PA Subwoofers | PA Subs | Stage Subwoofers | Seismic Audio Speakers\",\"metaTagDescription\":\"Learn more about Pro Audio Subwoofers at Seismic Audio. Take advantage of the lowest prices and best selection of PA Subwoofers on the web. We offer Active PA Subwoofers, Passive PA Subwoofers, Powered PA Subwoofers, Unpowered PA Subwoofers,  Studio Subwoofers, as well as replacement PA Subwoofers. Seismic Audio offers Free Shipping and orders ship the same day when placed by 2:30 CST.\",\"metaTagKeywords\":\"Subwoofer, powered, sub, cabinet, 18 inch, 15 inch, pro audio, quality, loudspeaker, PA, cabinet. sub, woofer, 18\\\" subwoofer, 15\\\" Subwoofer, studio Subwoofer, 18\\\" sub woofer, 15\\\" Sub woofer, studio Sub woofer,  18 inch subwoofer, 15 inch Subwoofer, studio Subwoofer, 18\\\" subwoofers, 15\\\" Subwoofers, studio Subwoofers,\",\"slug\":\"subwoofers\"},\"childrenCategories\":[],\"sequence\":2,\"isDisplayed\":true,\"categoryCode\":\"1053\",\"count\":0},\"content\":{\"categoryImages\":[],\"name\":\"Passive Subwoofers\",\"description\":\"\",\"pageTitle\":\"\",\"metaTagTitle\":\"Passive PA Subwoofer Cabinets | Unpowered Subwoofer Cabinets | Seismic Audio\",\"metaTagDescription\":\"Shop our passive subwoofers for all applications.  Finding the passive subwoofer cabinet that factors in the venue size and type of music is critical to your sound.  Order unpowered subwoofers from Seismic Audio and get free shipping!\",\"metaTagKeywords\":\"\",\"slug\":\"passive-subwoofers\"},\"childrenCategories\":[],\"sequence\":2,\"isDisplayed\":true,\"categoryCode\":\"1053\",\"count\":0}],\"measurements\":{\"packageHeight\":{\"unit\":\"in\",\"value\":23.000},\"packageWidth\":{\"unit\":\"in\",\"value\":21.000},\"packageLength\":{\"unit\":\"in\",\"value\":20.000},\"packageWeight\":{\"unit\":\"lbs\",\"value\":58.000}},\"isPackagedStandAlone\":false,\"properties\":[{\"attributeFQN\":\"Tenant~Model\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":331,\"name\":\"Model\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"Baby Tremor 15\",\"stringValue\":\"Baby Tremor 15\"}]},{\"attributeFQN\":\"tenant~availability\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"Predefined\",\"inputType\":\"List\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":1,\"name\":\"Availability\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"24hrs\",\"stringValue\":\"Usually Ships in 24 Hours\"}]},{\"attributeFQN\":\"Tenant~Contents\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextArea\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":248,\"name\":\"Contents\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"15\\\" Subwoofer Cabinet\",\"stringValue\":\"15\\\" Subwoofer Cabinet\"}]},{\"attributeFQN\":\"Tenant~Condition\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"Predefined\",\"inputType\":\"List\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":241,\"name\":\"Condition\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"New\",\"stringValue\":\"New\"}]},{\"attributeFQN\":\"Tenant~Application\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextArea\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":229,\"name\":\"Application\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"Subwoofer | PA Subwoofer | DJ Subwoofer\",\"stringValue\":\"Subwoofer | PA Subwoofer | DJ Subwoofer\"}]},{\"attributeFQN\":\"tenant~Use\",\"isHidden\":true,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"Predefined\",\"inputType\":\"List\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":444,\"name\":\"Use\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"Live_Sound\",\"stringValue\":\"Live Sound\"}]},{\"attributeFQN\":\"Tenant~Active-Passive\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"Predefined\",\"inputType\":\"List\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":221,\"name\":\"Active / Passive\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"Passive\",\"stringValue\":\"Passive\"}]},{\"attributeFQN\":\"Tenant~Connectors\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextArea\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":246,\"name\":\"Connector(s)\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"Two 1/4\\\" and Two Speakon\",\"stringValue\":\"Two 1/4\\\" and Two Speakon\"}]},{\"attributeFQN\":\"Tenant~LPF-Low-Pass-Filter\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":315,\"name\":\"LPF (Low Pass Filter)\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[]},{\"attributeFQN\":\"Tenant~Volume-Controls\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextArea\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":415,\"name\":\"Volume Controls\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[]},{\"attributeFQN\":\"tenant~Subwoofer_Sets\",\"isHidden\":true,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"Predefined\",\"inputType\":\"List\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":446,\"name\":\"Subwoofer Sets\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"Single\",\"stringValue\":\"Single\"}]},{\"attributeFQN\":\"Tenant~Power-Rating\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":373,\"name\":\"Power Rating\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"300 Watts RMS; 600 Watts Peak\",\"stringValue\":\"300 Watts RMS; 600 Watts Peak\"}]},{\"attributeFQN\":\"Tenant~Nominal-Impedance\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"Predefined\",\"inputType\":\"List\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":353,\"name\":\"Nominal Impedance\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"8-Ohms\",\"stringValue\":\"8 Ohms\"}]},{\"attributeFQN\":\"Tenant~Coverage-Pattern\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":254,\"name\":\"Coverage Pattern\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[]},{\"attributeFQN\":\"Tenant~Woofer-Specs\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextArea\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":424,\"name\":\"Woofer Specs\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[]},{\"attributeFQN\":\"Tenant~Speaker-Size\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"Predefined\",\"inputType\":\"List\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":438,\"name\":\"Speaker Size\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"15\",\"stringValue\":\"15\\\"\"}]},{\"attributeFQN\":\"Tenant~Magnet-Size\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":318,\"name\":\"Magnet Size\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"70 oz\",\"stringValue\":\"70 oz\"}]},{\"attributeFQN\":\"Tenant~Handles\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":290,\"name\":\"Handle(s)\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"Recessed Metal\",\"stringValue\":\"Recessed Metal\"}]},{\"attributeFQN\":\"Tenant~Voice-Coil\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextArea\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":423,\"name\":\"Voice Coil\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"3\\\" Voice Coil\",\"stringValue\":\"3\\\" Voice Coil\"}]},{\"attributeFQN\":\"Tenant~Frequency-Range\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":283,\"name\":\"Frequency Range\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[]},{\"attributeFQN\":\"Tenant~Frequency-Response\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":286,\"name\":\"Frequency Response\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"35 Hz - 800 Hz\",\"stringValue\":\"35 Hz - 800 Hz\"}]},{\"attributeFQN\":\"Tenant~Crossover-Frequency\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextArea\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":256,\"name\":\"Crossover Frequency\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[]},{\"attributeFQN\":\"Tenant~Sensitivity\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":408,\"name\":\"Sensitivity\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"96 dB\",\"stringValue\":\"96 dB\"}]},{\"attributeFQN\":\"Tenant~Maximum-SPL\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":336,\"name\":\"Maximum SPL\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"105 dB - Continuous\",\"stringValue\":\"105 dB - Continuous\"}]},{\"attributeFQN\":\"Tenant~Ports\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":363,\"name\":\"Ports\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"Yes\",\"stringValue\":\"Yes\"}]},{\"attributeFQN\":\"Tenant~Grill\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":300,\"name\":\"Grill\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"Full Metal Grill\",\"stringValue\":\"Full Metal Grill\"}]},{\"attributeFQN\":\"Tenant~Enclosure\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":274,\"name\":\"Enclosure\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"5/8\\\" Plywood Construction\",\"stringValue\":\"5/8\\\" Plywood Construction\"}]},{\"attributeFQN\":\"Tenant~Covering\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":253,\"name\":\"Covering\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"Black Carpet with Black Metal Corners\",\"stringValue\":\"Black Carpet with Black Metal Corners\"}]},{\"attributeFQN\":\"Tenant~Pole-Mount\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextArea\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":361,\"name\":\"Pole Mount\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"Yes\",\"stringValue\":\"Yes\"}]},{\"attributeFQN\":\"Tenant~Amplifier-Type\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":226,\"name\":\"Amplifier Type\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[]},{\"attributeFQN\":\"Tenant~Crossover-Modes\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":259,\"name\":\"Crossover Modes\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[]},{\"attributeFQN\":\"Tenant~Indicators\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextArea\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":308,\"name\":\"Indicators\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[]},{\"attributeFQN\":\"Tenant~Dimensions\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextArea\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":261,\"name\":\"Dimensions\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[]},{\"attributeFQN\":\"Tenant~Height\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":296,\"name\":\"Height\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"23.25\\\"\",\"stringValue\":\"23.25\\\"\"}]},{\"attributeFQN\":\"Tenant~Width\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":437,\"name\":\"Width\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"20.75\\\"\",\"stringValue\":\"20.75\\\"\"}]},{\"attributeFQN\":\"Tenant~Depth\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":257,\"name\":\"Depth\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"19.75\\\"\",\"stringValue\":\"19.75\\\"\"}]},{\"attributeFQN\":\"Tenant~Weight\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":435,\"name\":\"Weight\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"58 lbs per Cabinet\",\"stringValue\":\"58 lbs per Cabinet\"}]},{\"attributeFQN\":\"Tenant~Warranty\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"Predefined\",\"inputType\":\"List\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":418,\"name\":\"Warranty\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"One-Year-Warranty\",\"stringValue\":\"One Year Warranty\"}]},{\"attributeFQN\":\"Tenant~List-Price\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":314,\"name\":\"List Price\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"359.99\",\"stringValue\":\"359.99\"}]},{\"attributeFQN\":\"Tenant~Features\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextArea\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":279,\"name\":\"Features\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"Can be Daisy Chained\",\"stringValue\":\"Can be Daisy Chained\"}]},{\"attributeFQN\":\"Tenant~Additional-Features\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextArea\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":223,\"name\":\"Additional Features\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[]},{\"attributeFQN\":\"Tenant~Vendor-Price\",\"isHidden\":true,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":413,\"name\":\"Vendor Price\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"70\",\"stringValue\":\"70\"}]},{\"attributeFQN\":\"Tenant~Additional-Documents\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextArea\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":222,\"name\":\"Additional Documents\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[]},{\"attributeFQN\":\"Tenant~Technical-Specs\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextArea\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":402,\"name\":\"Technical Specs\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"<br/><span style=\\\"font-weight: bold;\\\">Frequency Re\",\"stringValue\":\"<br/><span style=\\\"font-weight: bold;\\\">Frequency Response Chart    </span><img src=\\\"http://d3d71ba2asa5oz.cloudfront.net/33000510/images/baby-tremorfrchart.jpg\\\" alt=\\\"Baby Tremor Frequency Response Chart\\\" align=\\\"\\\" border=\\\"0px\\\"/>\"}]},{\"attributeFQN\":\"Tenant~Video-Id\",\"isHidden\":true,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":417,\"name\":\"Video Id\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"wYtuvzvDka0\",\"stringValue\":\"wYtuvzvDka0\"}]},{\"attributeFQN\":\"Tenant~Manufacturer\",\"isHidden\":true,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"Predefined\",\"inputType\":\"List\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":321,\"name\":\"Manufacturer\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"Seismic-Audio\",\"stringValue\":\"Seismic Audio\"}]},{\"attributeFQN\":\"Tenant~Ships-By-Itself\",\"isHidden\":true,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"YesNo\",\"dataType\":\"Bool\",\"usageType\":\"Property\",\"dataTypeSequence\":11,\"name\":\"Ships By Itself\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":false}]},{\"attributeFQN\":\"Tenant~Oversized\",\"isHidden\":true,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"YesNo\",\"dataType\":\"Bool\",\"usageType\":\"Property\",\"dataTypeSequence\":10,\"name\":\"Oversized\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":false}]},{\"attributeFQN\":\"Tenant~Additional-Handling\",\"isHidden\":true,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"YesNo\",\"dataType\":\"Bool\",\"usageType\":\"Property\",\"dataTypeSequence\":7,\"name\":\"Additional Handling\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":false}]},{\"attributeFQN\":\"Tenant~Reward-Points\",\"isHidden\":true,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":394,\"name\":\"Reward Points\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[{\"value\":\"200\",\"stringValue\":\"200\"}]},{\"attributeFQN\":\"tenant~product-crosssell\",\"isHidden\":false,\"isMultiValue\":true,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":2,\"name\":\"Product Cross-Sells\"},\"values\":[{\"value\":\"SA-15T\",\"stringValue\":\"SA-15T\"},{\"value\":\"SA-12T\",\"stringValue\":\"SA-12T\"},{\"value\":\"Sub-Stand\",\"stringValue\":\"Sub-Stand\"},{\"value\":\"NPS-15\",\"stringValue\":\"NPS-15\"},{\"value\":\"TW12S25\",\"stringValue\":\"TW12S25\"},{\"value\":\"SASPT14-25\",\"stringValue\":\"SASPT14-25\"}]},{\"attributeFQN\":\"tenant~product-upsell\",\"isHidden\":false,\"isMultiValue\":true,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"String\",\"usageType\":\"Property\",\"dataTypeSequence\":3,\"name\":\"Product Upsells\"},\"values\":[]},{\"attributeFQN\":\"tenant~rating\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"Number\",\"usageType\":\"Property\",\"dataTypeSequence\":1,\"name\":\"Rating\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[]},{\"attributeFQN\":\"tenant~popularity\",\"isHidden\":false,\"isMultiValue\":false,\"attributeDetail\":{\"valueType\":\"AdminEntered\",\"inputType\":\"TextBox\",\"dataType\":\"Number\",\"usageType\":\"Property\",\"dataTypeSequence\":2,\"name\":\"Popularity\",\"description\":\"\",\"searchableInStorefront\":true},\"values\":[]}]}]}";
            var soc = new SimpleObjectContainer();
            engine.EnableCaseInsensitivePropertyLookups = true;
            engine.AddHostObject("clo", soc);
            engine.Evaluate("clo.So = { bag:{ 'a':1 , b:'c'} , price: 23.2}");
            //engine.AddHostObject("prodColString", prodColString);
            engine.Script.prodColString = prodColString;
            engine.Evaluate("var prodCol = JSON.parse(prodColString);");

            Assert.AreEqual(soc.SO.Price, (decimal)23.2);
           var code  =  engine.Compile("clo.So = prodCol.items[0]; ");

            engine.Evaluate("clo.So.V8Item =  {'a':1}");
            Assert.IsNotNull(soc.SO.V8Item);


        }






        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_NextTick()
        {
            // ReSharper disable AccessToDisposedClosure

            using (var innerEngine = new V8ScriptEngine())
            {
             
                var callBackTester = new CallbackTester();
                
                engine.AddHostObject("tester", callBackTester);
                engine.AddHostObject("process", new Process(engine));

                var scriptCallback = (dynamic)engine.Evaluate("g=function (){ process.nextTick( function(){tester.Callback(2); g=h/j;}); tester.Callback(1);}");

             
                var clinetCallbackWrapper = new Action(() =>
                {
                    scriptCallback();
                });
                engine.NextTick(clinetCallbackWrapper);

                callBackTester.Handle.WaitOne(10000);
                Assert.AreEqual((int)callBackTester.State, 1);
                callBackTester.Handle.WaitOne(10000);

                Assert.AreEqual((int)callBackTester.State , 2);

                //let loop die.


                //start new loop
                engine.NextTick(clinetCallbackWrapper);
                callBackTester.Handle.WaitOne(10000);
                Assert.AreEqual((int)callBackTester.State , 1);
                callBackTester.Handle.WaitOne(10000);

                Assert.AreEqual((int)callBackTester.State , 2);

                callBackTester.Handle.WaitOne(1000);
            }

            // ReSharper restore AccessToDisposedClosure
        }






        public class Process
        {
            private readonly V8ScriptEngine engine;

            public Process(V8ScriptEngine engine)
            {
                this.engine = engine;
            }

            public void nextTick(dynamic d)
            {
                this.engine.NextTick(d);
            }
        }

        public class CallbackTester
        {
            public AutoResetEvent Handle = new AutoResetEvent(false);
            public object State { get; set; }

            public void Callback(object state)
            {
                State = state;
                Handle.Set();
           
            }
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_EnableNullResultWrapping()
        {
            var testValue = new[] { 1, 2, 3, 4, 5 };
            engine.Script.host = new HostFunctions();
            engine.Script.foo = new NullResultWrappingTestObject<int[]>(testValue);

            Assert.IsFalse(Convert.ToBoolean(engine.Evaluate("foo.Value === null")));
            Assert.IsFalse(Convert.ToBoolean(engine.Evaluate("host.isNull(foo.Value)")));
            Assert.IsTrue(Convert.ToBoolean(engine.Evaluate("foo.NullValue === null")));
            Assert.IsTrue(Convert.ToBoolean(engine.Evaluate("host.isNull(foo.NullValue)")));
            Assert.IsFalse(Convert.ToBoolean(engine.Evaluate("foo.WrappedNullValue === null")));
            Assert.IsTrue(Convert.ToBoolean(engine.Evaluate("host.isNull(foo.WrappedNullValue)")));

            Assert.AreSame(testValue, engine.Evaluate("foo.Method(foo.Value)"));
            Assert.IsNull(engine.Evaluate("foo.Method(foo.WrappedNullValue)"));
            TestUtil.AssertException<RuntimeBinderException>(() => engine.Evaluate("foo.Method(foo.NullValue)"));

            engine.EnableNullResultWrapping = true;
            Assert.AreSame(testValue, engine.Evaluate("foo.Method(foo.Value)"));
            Assert.IsNull(engine.Evaluate("foo.Method(foo.WrappedNullValue)"));
            Assert.IsNull(engine.Evaluate("foo.Method(foo.NullValue)"));

            engine.EnableNullResultWrapping = false;
            Assert.AreSame(testValue, engine.Evaluate("foo.Method(foo.Value)"));
            Assert.IsNull(engine.Evaluate("foo.Method(foo.WrappedNullValue)"));
            TestUtil.AssertException<RuntimeBinderException>(() => engine.Evaluate("foo.Method(foo.NullValue)"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_EnableNullResultWrapping_String()
        {
            const string testValue = "bar";
            engine.Script.host = new HostFunctions();
            engine.Script.foo = new NullResultWrappingTestObject<string>(testValue);

            Assert.IsFalse(Convert.ToBoolean(engine.Evaluate("foo.Value === null")));
            Assert.IsFalse(Convert.ToBoolean(engine.Evaluate("host.isNull(foo.Value)")));
            Assert.IsTrue(Convert.ToBoolean(engine.Evaluate("foo.NullValue === null")));
            Assert.IsTrue(Convert.ToBoolean(engine.Evaluate("host.isNull(foo.NullValue)")));
            Assert.IsFalse(Convert.ToBoolean(engine.Evaluate("foo.WrappedNullValue === null")));
            Assert.IsTrue(Convert.ToBoolean(engine.Evaluate("host.isNull(foo.WrappedNullValue)")));

            Assert.AreEqual(testValue, engine.Evaluate("foo.Method(foo.Value)"));
            Assert.IsNull(engine.Evaluate("foo.Method(foo.WrappedNullValue)"));
            TestUtil.AssertException<RuntimeBinderException>(() => engine.Evaluate("foo.Method(foo.NullValue)"));

            engine.EnableNullResultWrapping = true;
            Assert.AreEqual(testValue, engine.Evaluate("foo.Method(foo.Value)"));
            Assert.IsNull(engine.Evaluate("foo.Method(foo.WrappedNullValue)"));
            Assert.IsNull(engine.Evaluate("foo.Method(foo.NullValue)"));

            engine.EnableNullResultWrapping = false;
            Assert.AreEqual(testValue, engine.Evaluate("foo.Method(foo.Value)"));
            Assert.IsNull(engine.Evaluate("foo.Method(foo.WrappedNullValue)"));
            TestUtil.AssertException<RuntimeBinderException>(() => engine.Evaluate("foo.Method(foo.NullValue)"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_EnableNullResultWrapping_Nullable()
        {
            int? testValue = 12345;
            engine.Script.host = new HostFunctions();
            engine.Script.foo = new NullResultWrappingTestObject<int?>(testValue);

            Assert.IsFalse(Convert.ToBoolean(engine.Evaluate("foo.Value === null")));
            Assert.IsFalse(Convert.ToBoolean(engine.Evaluate("host.isNull(foo.Value)")));
            Assert.IsTrue(Convert.ToBoolean(engine.Evaluate("foo.NullValue === null")));
            Assert.IsTrue(Convert.ToBoolean(engine.Evaluate("host.isNull(foo.NullValue)")));
            Assert.IsFalse(Convert.ToBoolean(engine.Evaluate("foo.WrappedNullValue === null")));
            Assert.IsTrue(Convert.ToBoolean(engine.Evaluate("host.isNull(foo.WrappedNullValue)")));

            Assert.AreEqual(testValue, engine.Evaluate("foo.Method(foo.Value)"));
            Assert.IsNull(engine.Evaluate("foo.Method(foo.WrappedNullValue)"));
            TestUtil.AssertException<RuntimeBinderException>(() => engine.Evaluate("foo.Method(foo.NullValue)"));

            engine.EnableNullResultWrapping = true;
            Assert.AreEqual(testValue, engine.Evaluate("foo.Method(foo.Value)"));
            Assert.IsNull(engine.Evaluate("foo.Method(foo.WrappedNullValue)"));
            Assert.IsNull(engine.Evaluate("foo.Method(foo.NullValue)"));

            engine.EnableNullResultWrapping = false;
            Assert.AreEqual(testValue, engine.Evaluate("foo.Method(foo.Value)"));
            Assert.IsNull(engine.Evaluate("foo.Method(foo.WrappedNullValue)"));
            TestUtil.AssertException<RuntimeBinderException>(() => engine.Evaluate("foo.Method(foo.NullValue)"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DefaultProperty()
        {
            engine.Script.foo = new DefaultPropertyTestObject();
            engine.AddHostType("DayOfWeek", typeof(DayOfWeek));

            engine.Execute("foo.Item.set('ghi', 321)");
            Assert.AreEqual(321, engine.Evaluate("foo('ghi')"));
            Assert.AreEqual(321, engine.Evaluate("foo.Item('ghi')"));
            Assert.AreEqual(321, engine.Evaluate("foo.Item.get('ghi')"));
            Assert.IsNull(engine.Evaluate("foo('jkl')"));

            engine.Execute("foo.Item.set(DayOfWeek.Saturday, -123)");
            Assert.AreEqual(-123, engine.Evaluate("foo(DayOfWeek.Saturday)"));
            Assert.AreEqual(-123, engine.Evaluate("foo.Item(DayOfWeek.Saturday)"));
            Assert.AreEqual(-123, engine.Evaluate("foo.Item.get(DayOfWeek.Saturday)"));
            Assert.IsNull(engine.Evaluate("foo(DayOfWeek.Sunday)"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DefaultProperty_FieldTunneling()
        {
            engine.Script.foo = new DefaultPropertyTestContainer();
            engine.AddHostType("DayOfWeek", typeof(DayOfWeek));

            engine.Execute("foo.Field.Item.set('ghi', 321)");
            Assert.AreEqual(321, engine.Evaluate("foo.Field('ghi')"));
            Assert.AreEqual(321, engine.Evaluate("foo.Field.Item('ghi')"));
            Assert.AreEqual(321, engine.Evaluate("foo.Field.Item.get('ghi')"));
            Assert.IsNull(engine.Evaluate("foo.Field('jkl')"));

            engine.Execute("foo.Field.Item.set(DayOfWeek.Saturday, -123)");
            Assert.AreEqual(-123, engine.Evaluate("foo.Field(DayOfWeek.Saturday)"));
            Assert.AreEqual(-123, engine.Evaluate("foo.Field.Item(DayOfWeek.Saturday)"));
            Assert.AreEqual(-123, engine.Evaluate("foo.Field.Item.get(DayOfWeek.Saturday)"));
            Assert.IsNull(engine.Evaluate("foo.Field(DayOfWeek.Sunday)"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DefaultProperty_PropertyTunneling()
        {
            engine.Script.foo = new DefaultPropertyTestContainer();
            engine.AddHostType("DayOfWeek", typeof(DayOfWeek));

            engine.Execute("foo.Property.Item.set('ghi', 321)");
            Assert.AreEqual(321, engine.Evaluate("foo.Property('ghi')"));
            Assert.AreEqual(321, engine.Evaluate("foo.Property.Item('ghi')"));
            Assert.AreEqual(321, engine.Evaluate("foo.Property.Item.get('ghi')"));
            Assert.IsNull(engine.Evaluate("foo.Property('jkl')"));

            engine.Execute("foo.Property.Item.set(DayOfWeek.Saturday, -123)");
            Assert.AreEqual(-123, engine.Evaluate("foo.Property(DayOfWeek.Saturday)"));
            Assert.AreEqual(-123, engine.Evaluate("foo.Property.Item(DayOfWeek.Saturday)"));
            Assert.AreEqual(-123, engine.Evaluate("foo.Property.Item.get(DayOfWeek.Saturday)"));
            Assert.IsNull(engine.Evaluate("foo.Property(DayOfWeek.Sunday)"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DefaultProperty_MethodTunneling()
        {
            engine.Script.foo = new DefaultPropertyTestContainer();
            engine.AddHostType("DayOfWeek", typeof(DayOfWeek));

            engine.Execute("foo.Method().Item.set('ghi', 321)");
            Assert.AreEqual(321, engine.Evaluate("foo.Method()('ghi')"));
            Assert.AreEqual(321, engine.Evaluate("foo.Method().Item('ghi')"));
            Assert.AreEqual(321, engine.Evaluate("foo.Method().Item.get('ghi')"));
            Assert.IsNull(engine.Evaluate("foo.Method()('jkl')"));

            engine.Execute("foo.Method().Item.set(DayOfWeek.Saturday, -123)");
            Assert.AreEqual(-123, engine.Evaluate("foo.Method()(DayOfWeek.Saturday)"));
            Assert.AreEqual(-123, engine.Evaluate("foo.Method().Item(DayOfWeek.Saturday)"));
            Assert.AreEqual(-123, engine.Evaluate("foo.Method().Item.get(DayOfWeek.Saturday)"));
            Assert.IsNull(engine.Evaluate("foo.Method()(DayOfWeek.Sunday)"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_DefaultProperty_Indexer()
        {
            engine.Script.dict = new Dictionary<string, object> { { "abc", 123 }, { "def", 456 }, { "ghi", 789 } };
            engine.Execute("item = dict.Item");

            Assert.AreEqual(123, engine.Evaluate("item('abc')"));
            Assert.AreEqual(456, engine.Evaluate("item('def')"));
            Assert.AreEqual(789, engine.Evaluate("item('ghi')"));
            TestUtil.AssertException<KeyNotFoundException>(() => engine.Evaluate("item('jkl')"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_PropertyAndMethodWithSameName()
        {
            engine.AddHostObject("lib", HostItemFlags.GlobalMembers, new HostTypeCollection("mscorlib", "System", "System.Core"));

            engine.Script.dict = new Dictionary<string, object> { { "abc", 123 }, { "def", 456 }, { "ghi", 789 } };
            Assert.AreEqual(3, engine.Evaluate("dict.Count"));
            TestUtil.AssertException<ScriptEngineException>(() => engine.Evaluate("dict.Count()"));

            engine.Script.listDict = new ListDictionary { { "abc", 123 }, { "def", 456 }, { "ghi", 789 } };
            Assert.AreEqual(3, engine.Evaluate("listDict.Count"));
            TestUtil.AssertException<ScriptEngineException>(() => engine.Evaluate("listDict.Count()"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_toFunction_Delegate()
        {
            engine.Script.foo = new Func<int, double>(arg => arg * Math.PI);
            Assert.AreEqual(123 * Math.PI, engine.Evaluate("foo(123)"));
            Assert.AreEqual("function", engine.Evaluate("typeof foo.toFunction"));
            Assert.AreEqual("function", engine.Evaluate("typeof foo.toFunction()"));
            Assert.AreEqual(456 * Math.PI, engine.Evaluate("foo.toFunction()(456)"));
            TestUtil.AssertException<ScriptEngineException>(() => engine.Evaluate("new foo()"));
            TestUtil.AssertException<ScriptEngineException>(() => engine.Evaluate("new (foo.toFunction())()"));

            engine.Script.bar = new VarArgDelegate((pre, args) => args.Aggregate((int)pre, (value, arg) => value + (int)arg));
            Assert.AreEqual(3330, engine.Evaluate("bar(123, 456, 789, 987, 654, 321)"));
            Assert.AreEqual("function", engine.Evaluate("typeof bar.toFunction"));
            Assert.AreEqual("function", engine.Evaluate("typeof bar.toFunction()"));
            Assert.AreEqual(2934, engine.Evaluate("bar.toFunction()(135, 579, 975, 531, 135, 579)"));
            TestUtil.AssertException<ScriptEngineException>(() => engine.Evaluate("new bar()"));
            TestUtil.AssertException<ScriptEngineException>(() => engine.Evaluate("new (bar.toFunction())()"));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_toFunction_Method()
        {
            engine.Script.host = new HostFunctions();
            Assert.AreEqual("function", engine.Evaluate("typeof host.newObj.toFunction"));
            Assert.AreEqual("function", engine.Evaluate("typeof host.newObj.toFunction()"));
            Assert.IsInstanceOfType(engine.Evaluate("host.newObj()"), typeof(PropertyBag));
            Assert.IsInstanceOfType(engine.Evaluate("host.newObj.toFunction()()"), typeof(PropertyBag));
            TestUtil.AssertException<ScriptEngineException>(() => engine.Evaluate("new host.newObj()"));
            TestUtil.AssertException<ScriptEngineException>(() => engine.Evaluate("new (host.newObj.toFunction())()"));

            engine.AddHostType(typeof(Random));
            Assert.IsInstanceOfType(engine.Evaluate("host.newObj(Random, 100)"), typeof(Random));
            Assert.IsInstanceOfType(engine.Evaluate("host.newObj.toFunction()(Random, 100)"), typeof(Random));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_toFunction_Type()
        {
            engine.AddHostType(typeof(Random));
            Assert.AreEqual("function", engine.Evaluate("typeof Random.toFunction"));
            Assert.AreEqual("function", engine.Evaluate("typeof Random.toFunction()"));
            Assert.IsInstanceOfType(engine.Evaluate("new Random()"), typeof(Random));
            Assert.IsInstanceOfType(engine.Evaluate("new Random(100)"), typeof(Random));
            Assert.IsInstanceOfType(engine.Evaluate("new (Random.toFunction())()"), typeof(Random));
            Assert.IsInstanceOfType(engine.Evaluate("new (Random.toFunction())(100)"), typeof(Random));
            TestUtil.AssertException<ScriptEngineException>(() => engine.Evaluate("Random(100)"));
            TestUtil.AssertException<ScriptEngineException>(() => engine.Evaluate("(Random.toFunction())(100)"));

            engine.AddHostType(typeof(Dictionary<,>));
            engine.AddHostType(typeof(int));
            Assert.AreEqual("function", engine.Evaluate("typeof Dictionary.toFunction"));
            Assert.AreEqual("function", engine.Evaluate("typeof Dictionary.toFunction()"));
            Assert.IsInstanceOfType(engine.Evaluate("Dictionary(Int32, Int32)"), typeof(HostType));
            Assert.IsInstanceOfType(engine.Evaluate("Dictionary.toFunction()(Int32, Int32)"), typeof(HostType));
            Assert.IsInstanceOfType(engine.Evaluate("new Dictionary(Int32, Int32, 100)"), typeof(Dictionary<int, int>));
            Assert.IsInstanceOfType(engine.Evaluate("new (Dictionary.toFunction())(Int32, Int32, 100)"), typeof(Dictionary<int, int>));
        }

        [TestMethod, TestCategory("V8ScriptEngine")]
        public void V8ScriptEngine_toFunction_None()
        {
            engine.Script.foo = new Random();
            Assert.IsInstanceOfType(engine.Evaluate("foo"), typeof(Random));
            Assert.IsInstanceOfType(engine.Evaluate("foo.toFunction"), typeof(Undefined));
        }

        // ReSharper restore InconsistentNaming

        #endregion

        #region miscellaneous

        private const string generalScript =
        @"
            System = clr.System;

            TestObject = host.type('Microsoft.ClearScript.Test.GeneralTestObject', 'ClearScriptTest');
            tlist = host.newObj(System.Collections.Generic.List(TestObject));
            tlist.Add(host.newObj(TestObject, 'Eóin', 20));
            tlist.Add(host.newObj(TestObject, 'Shane', 16));
            tlist.Add(host.newObj(TestObject, 'Cillian', 8));
            tlist.Add(host.newObj(TestObject, 'Sasha', 6));
            tlist.Add(host.newObj(TestObject, 'Brian', 3));

            olist = host.newObj(System.Collections.Generic.List(System.Object));
            olist.Add({ name: 'Brian', age: 3 });
            olist.Add({ name: 'Sasha', age: 6 });
            olist.Add({ name: 'Cillian', age: 8 });
            olist.Add({ name: 'Shane', age: 16 });
            olist.Add({ name: 'Eóin', age: 20 });

            dict = host.newObj(System.Collections.Generic.Dictionary(System.String, System.String));
            dict.Add('foo', 'bar');
            dict.Add('baz', 'qux');
            value = host.newVar(System.String);
            result = dict.TryGetValue('foo', value.out);

            bag = host.newObj();
            bag.method = function (x) { System.Console.WriteLine(x * x); };
            bag.proc = host.del(System.Action(System.Object), bag.method);

            expando = host.newObj(System.Dynamic.ExpandoObject);
            expandoCollection = host.cast(System.Collections.Generic.ICollection(System.Collections.Generic.KeyValuePair(System.String, System.Object)), expando);

            function onChange(s, e) {
                System.Console.WriteLine('Property changed: {0}; new value: {1}', e.PropertyName, s[e.PropertyName]);
            };
            function onStaticChange(s, e) {
                System.Console.WriteLine('Property changed: {0}; new value: {1} (static event)', e.PropertyName, e.PropertyValue);
            };
            eventCookie = tlist.Item(0).Change.connect(onChange);
            staticEventCookie = TestObject.StaticChange.connect(onStaticChange);
            tlist.Item(0).Name = 'Jerry';
            tlist.Item(1).Name = 'Ellis';
            tlist.Item(0).Name = 'Eóin';
            tlist.Item(1).Name = 'Shane';
            eventCookie.disconnect();
            staticEventCookie.disconnect();
            tlist.Item(0).Name = 'Jerry';
            tlist.Item(1).Name = 'Ellis';
            tlist.Item(0).Name = 'Eóin';
            tlist.Item(1).Name = 'Shane';
        ";

        private const string generalScriptOutput =
        @"
            Property changed: Name; new value: Jerry
            Property changed: Name; new value: Jerry (static event)
            Property changed: Name; new value: Ellis (static event)
            Property changed: Name; new value: Eóin
            Property changed: Name; new value: Eóin (static event)
            Property changed: Name; new value: Shane (static event)
        ";

        public object TestProperty { get; set; }

        public static object StaticTestProperty { get; set; }

        // ReSharper disable UnusedMember.Local

        private void PrivateMethod()
        {
        }

        private static void PrivateStaticMethod()
        {
        }

        private delegate string TestDelegate(string pre, ref string value, int post);

        public delegate object VarArgDelegate(object pre, params object[] args);

        // ReSharper restore UnusedMember.Local

        #endregion
    }

    public static class TestExtensions
    {
        public static bool IsList(this object obj)
        {
            if (obj == null)
            {
                return false;
            }
            return obj.GetType().GetInterfaces().Any(x =>
                x.IsGenericType &&
                x.GetGenericTypeDefinition() == typeof(IList<>));
        }

        public static string toCSJSON(this object obj)
        {
            StringWriter sw = new StringWriter();
             Newtonsoft.Json.JsonSerializer.CreateDefault().Serialize(sw, obj);
            sw.Flush();
            return sw.GetStringBuilder().ToString();
        }
    }
}
