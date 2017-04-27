# EPEx
.Net extension that allows you to make a shallow and deep copy of an object.

## Remark
Deep copying an object, reference loops are taken into account.

# Important
## Remark 2
**Creating** any copy of (shallow, deep) object **does not cause any constructor**.

## Description
This library allows you to easily and quickly create a copy of the object, including **all the public, hidden and internal** fields and properties of the object (or structure).
For each type of object, during the operation, the **MSIL** code is generated, compiled and cached to the delegate function.

When the object is shallow copied, only the object is copied, all the reference (objects) of the field and the properties remain the same as for the original object. 
If deep copying of the object takes place, then all the properties and fields of the object are also copied, while the rule is observed: one object-one copy, i.e. If the original object contains a link to itself or to an object copied earlier, within the current deep copy, then the copied object will also be in a single instance.

### Example:
```csharp
using System;

namespace EP.Ex.Tests
{
        public class TestClass
        {
            public string Str;
            private string f{ get; set; }
            public TestClass(string tf)
            {
                Str = "Test Str";
                f= tf;
            }
        }
        public class Example{
            public static void Test(){
              object[] arr = new object[2] { 1, "str" };
              var obj1 = arr.ShallowCopy();
              //or
              var obj2 = Obj<object[]>.ShallowCopy(arr);
              var tc = new TestClass("MegaTest");
              var dtc=tc.DeepCopy();
              //or
              var dtc1 = Obj.DeepCopy(tc);
              ///type valued
              var five = (5).ShallowCopy();
            }
        }
}
```  

#### Another nice addition is the ability to complete cast to the parent class:
```csharp
  var child=new ChildClass();
  var obj=Obj<ParentClass>.ShallowCopy(child);
  ///obj.GetType() is ParentClass, not ChildClass =)
```  
#### Fast create new object using constructor without parameters
```csharp
   var s = Obj<struct1>.New();
   var s1 = Obj.New<struct1>();
   var s2 = (struct1)Obj.New(typeof(struct1));
   var i = Obj.New<Int32>();//==default(int)
```

For more examples, please see [TestFile](EP.Ex.Tests/ObjTests.cs)
## Features

You can override the method of creating a deep copy of an object, using **_SetDeepCopyFn_**.

### Example:
```csharp
        public static Dictionary<object, object> CloneDict(Dictionary<object, object> src, Dictionary<object, object> dict)
        {
            var dst = new Dictionary<object, object>();
            object key;
            object value;
            foreach (var p in src)
            {
                if (!dict.TryGetValue(p.Key, out key))
                {
                    key = dict[p.Key] = p.Key.DeepCopy();
                }
                if (!dict.TryGetValue(p.Value, out value))
                {
                    value = dict[p.Value] = p.Value.DeepCopy();
                }
                dst[key] = value;
            }
            return dst;
        }
        
        Obj<Dictionary<object, object>>.SetDeepCopyFn(CloneDict);
        ///now its use CloneDict instead of default
        var dictcopy = srcdict.DeepCopy();
```  
## Limitation
This library has a number of limitations.
The library can not make deep copies of:
* Unmanaged,
* Descriptors,
* Objects with tricky logic, such as indexing the object with HASH.

Thats why it has method that override default generated method.
Also, some of the deep copy methods (such as copying a dictionary, hashset and etc) are already described in advance in a file [CopyBaseHelper](EP.Ex/CopyBaseHelper.cs)

## Suggestions
If you have any suggestions you can always contact me and we can always discuss and, if necessary, supplement or fix it.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
