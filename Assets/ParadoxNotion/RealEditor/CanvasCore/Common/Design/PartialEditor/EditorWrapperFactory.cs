#if UNITY_EDITOR

using System;
using System.Reflection;

namespace ParadoxNotion.Design
{

    ///Factory for EditorObjectWrappers
    public static class EditorWrapperFactory
    {

        private static WeakReferenceTable<object, EditorObjectWrapper> cachedEditors = new WeakReferenceTable<object, EditorObjectWrapper>();

        ///Returns a cached EditorObjectWrapper of type T for target object
        public static T GetEditor<T>(object target) where T : EditorObjectWrapper
        {
            EditorObjectWrapper wrapper;
            if (cachedEditors.TryGetValueWithRefCheck(target, out wrapper))
            {
                return (T)wrapper;
            }
            wrapper = (T)(typeof(T).CreateObject());
            wrapper.Enable(target);
            cachedEditors.Add(target, wrapper);
            return (T)wrapper;
        }
    }

    ///----------------------------------------------------------------------------------------------

    ///Wrapper Editor for objects
    public abstract class EditorObjectWrapper : IDisposable
    {

        private WeakReference<object> _targetRef;
        ///The target
        public object target
        {
            get
            {
                _targetRef.TryGetTarget(out object reference);
                return reference;
            }
        }

        //...
        void IDisposable.Dispose() { OnDisable(); }

        ///Init for target
        public void Enable(object target)
        {
            _targetRef = new WeakReference<object>(target);
            OnEnable();
        }

        ///Create Property and Method wrappers here or other stuff.
        protected virtual void OnEnable() { }
        ///Cleanup
        protected virtual void OnDisable() { }

        ///Get a wrapped editor serialized field on target
        public EditorPropertyWrapper<T> CreatePropertyWrapper<T>(string name)
        {
            Type type = target.GetType();
            FieldInfo field = type.RTGetField(name, /*include private base*/ true);
            if (field != null)
            {
                EditorPropertyWrapper<T> wrapper = (EditorPropertyWrapper<T>)typeof(EditorPropertyWrapper<>).MakeGenericType(typeof(T)).CreateObject();
                wrapper.Init(this, field);
                return wrapper;
            }
            return null;
        }

        ///Get a wrapped editor method on target
        public EditorMethodWrapper CreateMethodWrapper(string name)
        {
            Type type = target.GetType();
            MethodInfo method = type.RTGetMethod(name);
            if (method != null)
            {
                EditorMethodWrapper wrapper = new EditorMethodWrapper();
                wrapper.Init(this, method);
                return wrapper;
            }
            return null;
        }
    }

    ///Wrapper Editor for objects
    public abstract class EditorObjectWrapper<T> : EditorObjectWrapper
    {
        public new T target { get { return (T)base.target; } }
    }

    ///----------------------------------------------------------------------------------------------

    ///An editor wrapped field
    public sealed class EditorPropertyWrapper<T>
    {
        private EditorObjectWrapper editor { get; set; }
        private FieldInfo field { get; set; }
        public T value
        {
            get
            {
                object o = field.GetValue(editor.target);
                return o != null ? (T)o : default(T);
            }
            set
            {
                field.SetValue(editor.target, value);
            }
        }

        public void Init(EditorObjectWrapper editor, FieldInfo field)
        {
            this.editor = editor;
            this.field = field;
        }
    }

    ///----------------------------------------------------------------------------------------------

    ///An editor wrapped method
    public sealed class EditorMethodWrapper
    {
        private EditorObjectWrapper editor { get; set; }
        private MethodInfo method { get; set; }
        public void Invoke(params object[] args)
        {
            method.Invoke(editor.target, args);
        }
        public void Init(EditorObjectWrapper editor, MethodInfo method)
        {
            this.editor = editor;
            this.method = method;
        }
    }
}

#endif