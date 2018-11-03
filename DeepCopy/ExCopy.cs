using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

public static class ExCopy
{
    static int bigDeep = 10;
    static Type stringType = typeof(string);
    static Type delType = typeof(MulticastDelegate);
    static MethodInfo[] arrayCopyMethodInfo;
    static MethodInfo objCopyMethoInfo = typeof(ExCopy).GetMethod(nameof(CopySame1), BindingFlags.NonPublic | BindingFlags.Static);
    static Dictionary<Type, MethodInfo> funStore = new Dictionary<Type, MethodInfo>();
    static ExCopy() {
        arrayCopyMethodInfo = new MethodInfo[3];
        for (int i = 0; i < arrayCopyMethodInfo.Length; i++) {
            arrayCopyMethodInfo[i] = typeof(ExCopy).GetMethod("arrayCopy" + (i + 1), BindingFlags.Static | BindingFlags.NonPublic);
        }
    }
    public static void DeepCopyCase<T, T1>(this T inObj, out T1 outObj) where T : new() where T1 : new() {
        if (inObj == null) throw new Exception("拷贝对象不能为空");
        outObj = FunCache<T, T1>.fun(inObj);
    }
    public static T DeepCopy<T>(this T inObj) {
        if (inObj == null) throw new Exception("拷贝对象不能为空");
        var type = inObj.GetType();
        if (type == typeof(T))
            return FunCache<T>.fun(inObj);
        else {
            if (funStore.TryGetValue(type, out MethodInfo v)) {
            }
            else {
                v = typeof(FunCache<>).MakeGenericType(type).GetMethod("Get", BindingFlags.NonPublic | BindingFlags.Static);
                funStore.Add(type, v);
            }
            return (T)v.Invoke(null, new object[] { inObj });
        }
    }
    //最大递归深度
    static bool IsSpecialValue(Type t) {
        return t.IsPrimitive || t.BaseType == delType || t == stringType||t==typeof(object);
    }
    static class FunCache<T>
    {
        public static Func<T, T> fun;
        static FunCache() {
            var type = typeof(T);
            var inParamer = Expression.Parameter(type);
            var exp = Copy(type, inParamer);
            fun = Expression.Lambda<Func<T, T>>(exp, inParamer).Compile();
        }
        static object Get(object o) {
            return fun((T)o);
        }
    }
    static class FunCache<T, T1>
    {
        public static Func<T, T1> fun;
        static FunCache() {
            var inType = typeof(T);
            var outType = typeof(T1);
            var inParamer = Expression.Parameter(inType);
            var exp = inType == outType ? Copy(inType, inParamer) : Copy(inType, outType, inParamer);
            fun = Expression.Lambda<Func<T, T1>>(exp, inParamer).Compile();
        }
        static object Get(object o) {
            return fun((T)o);
        }
    }
    
    static T CopySame1<T>(T inObj) {
        return FunCache<T>.fun(inObj);
    }
    static T[] arrayCopy1<T>(T[] t, bool isvalue) {
        var narray = new T[t.Length];
        for (int i = 0; i < t.Length; i++) {
            if (isvalue)
                narray[i] = t[i];
            else if (t[i] != null) {
                narray[i] = CopySame1(t[i]);
            }
        }
        return narray;
    }
    static T[,] arrayCopy2<T>(T[,] t, bool isvalue) {
        var len1 = t.GetLength(0);
        var len2 = t.GetLength(1);
        var narray = new T[len1, len2];
        for (int i = 0; i < len1; i++) {
            for (int ii = 0; ii < len2; ii++) {
                if (isvalue)
                    narray[i, ii] = t[i, ii];
                else if (t[i, ii] != null) {
                    narray[i, ii] = CopySame1(t[i, ii]);
                }
            }
        }
        return narray;
    }
    static T[,,] arrayCopy3<T>(T[,,] t, bool isvalue) {
        var len1 = t.GetLength(0);
        var len2 = t.GetLength(1);
        var len3 = t.GetLength(2);
        var narray = new T[len1, len2, len3];
        for (int i = 0; i < len1; i++) {
            for (int ii = 0; ii < len2; ii++) {
                for (int iii = 0; iii < len3; iii++) {
                    if (isvalue)
                        narray[i, ii, iii] = t[i, ii, iii];
                    else if (t[i, ii, iii] != null) {
                        narray[i, ii, iii] = CopySame1(t[i, ii, iii]);
                    }
                }
            }
        }
        return narray;
    }
    static Expression Copy(Type inType, Type outType, Expression inParameter, int deep = 0) {
        if (inType == outType && IsSpecialValue(inType) || deep >= bigDeep) {
            return inParameter;
        }
        if (outType.IsAbstract) {
            if (deep == 0) {
                if (outType.IsAssignableFrom(inType)) return inParameter;
                return Expression.Constant(null, outType);
            }
            else return inParameter;
        }
        var outParamer = Expression.Variable(outType);
        var assignList = new List<Expression>();
        if (inType.IsArray) {
            if (outType.IsArray && inType.GetElementType() == outType.GetElementType()) {
                int select = 0;
                if (inType.IsVariableBoundArray) {//如果是[,]数组
                    if (!outType.IsVariableBoundArray || inType.GetArrayRank() != outType.GetArrayRank()) { return Expression.Constant(null, outType); }
                    else if (inType.GetArrayRank() > arrayCopyMethodInfo.Length) throw new Exception($"不支持{arrayCopyMethodInfo.Length}维以上的数组");
                    else select = inType.GetArrayRank() - 1;
                }
                var arrayCreate = Expression.Call(arrayCopyMethodInfo[select].MakeGenericMethod(inType.GetElementType()), inParameter, Expression.Constant(IsSpecialValue(inType.GetElementType())));
                assignList.Add(Expression.Assign(outParamer, arrayCreate));
            }
        }
        else {
            if (outType.IsClass) {
                var ctor = outType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();
                if (ctor == null) throw new Exception(outType.Name + "没有构造函数");
                var pars = ctor.GetParameters();
                var parList = new List<Expression>();
                foreach (var item in pars) {
                    if (item.ParameterType.IsValueType)
                        parList.Add(Expression.New(item.ParameterType));
                    else
                        parList.Add(Expression.Constant(null, item.ParameterType));
                }
                var outNew = Expression.Assign(outParamer, Expression.New(ctor, parList));
                assignList.Add(outNew);
            }
            else {
                var outNew = Expression.Assign(outParamer, Expression.New(outType));
                assignList.Add(outNew);
            }
            do {
                foreach (var outFiled in outType.GetRuntimeFields()) {
                    if (outFiled.IsStatic || outFiled.IsLiteral) continue;
                    var inField = inType.GetRuntimeFields().Where(x => x.Name == outFiled.Name).FirstOrDefault();
                    if (inField != null && !(inField.IsStatic || inField.IsLiteral) && inField.FieldType == outFiled.FieldType) {
                        var outParamerField = Expression.Field(outParamer, outFiled);
                        var inParamerField = Expression.Field(inParameter, inField);
                        if (IsSpecialValue(outFiled.FieldType)) {
                            assignList.Add(
                               Expression.Assign(outParamerField, inParamerField)
                           );
                        }
                        else if (!outFiled.FieldType.IsValueType) {//引用类型时候判断是否为空
                            assignList.Add(
                                Expression.IfThen(Expression.NotEqual(inParamerField, Expression.Constant(null, inField.FieldType)),
                                Expression.Assign(outParamerField, Copy(inField.FieldType, inParamerField, deep + 1)))
                            );
                        }
                        else {
                            assignList.Add(
                                Expression.Assign(outParamerField, Copy(inField.FieldType, inParamerField, deep + 1))
                            );
                        }
                    }
                }
                outType = outType.BaseType;
                inType = inType.BaseType;
            } while (outType != null && outType == inType && inType != typeof(object));
        }
        assignList.Add(outParamer);
        return Expression.Block(new[] { outParamer }, assignList);
    }
    static Expression Copy(Type inType, Expression inParameter, int deep = 0) {
        if (IsSpecialValue(inType) || deep >= bigDeep) {
            return inParameter;
        }
        if (inType.IsAbstract) {
            if (deep == 0) {
                return inParameter;
            }
            else return inParameter;
        }
        var outParamer = Expression.Variable(inType);
        var assignList = new List<Expression>();
        if (inType.IsArray) {
            int select = 0;
            if (inType.IsVariableBoundArray) {//如果是[,]数组
                if (inType.GetArrayRank() > arrayCopyMethodInfo.Length) throw new Exception($"不支持{arrayCopyMethodInfo.Length}维以上的数组");
                else select = inType.GetArrayRank() - 1;
            }
            var arrayCreate = Expression.Call(arrayCopyMethodInfo[select].MakeGenericMethod(inType.GetElementType()), inParameter, Expression.Constant(IsSpecialValue(inType.GetElementType())));
            assignList.Add(Expression.Assign(outParamer, arrayCreate));
        }
        else {
            if (inType.IsClass) {
                var ctor = inType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();
                if (ctor == null) throw new Exception(inType.Name + "没有构造函数");
                var pars = ctor.GetParameters();
                var parList = new List<Expression>();
                foreach (var item in pars) {
                    if (item.ParameterType.IsValueType)
                        parList.Add(Expression.New(item.ParameterType));
                    else
                        parList.Add(Expression.Constant(null, item.ParameterType));
                }
                var outNew = Expression.Assign(outParamer, Expression.New(ctor, parList));
                assignList.Add(outNew);
            }
            else {
                var outNew = Expression.Assign(outParamer, Expression.New(inType));
                assignList.Add(outNew);
            }
            do {
                foreach (var outFiled in inType.GetRuntimeFields()) {
                    if (outFiled.IsStatic || outFiled.IsLiteral) continue;
                    var outParamerField = Expression.Field(outParamer, outFiled);
                    var inParamerField = Expression.Field(inParameter, outFiled);
                    if (IsSpecialValue(outFiled.FieldType)) {
                        assignList.Add(
                           Expression.Assign(outParamerField, inParamerField)
                       );
                    }
                    else if (!outFiled.FieldType.IsValueType) {//引用类型时候判断是否为空
                        assignList.Add(
                            Expression.IfThen(Expression.NotEqual(inParamerField, Expression.Constant(null, outFiled.FieldType)),
                            Expression.Assign(outParamerField, Copy(outFiled.FieldType, inParamerField, deep + 1)))
                        );
                    }
                    else {
                        assignList.Add(
                            Expression.Assign(outParamerField, Copy(outFiled.FieldType, inParamerField, deep + 1))
                        );
                    }
                }
                inType = inType.BaseType;
            } while (inType != null && inType != typeof(object));
        }
        assignList.Add(outParamer);
        return Expression.Block(new[] { outParamer }, assignList);
    }
}

