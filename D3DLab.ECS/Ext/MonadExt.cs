using System;
using System.Collections.Generic;
using System.Linq;

namespace D3DLab.ECS.Ext {
    public static class MonadExt {
        public static bool IsNull<T>(this T source) where T : class { return source == null; }
        public static bool IsNotNull<T>(this T source) where T : class { return source != null; }

        public static T AsDo<T>(this object _source, Action<T> action) where T : class {
            var source = _source as T;
            if (source == null) {
                return null;
            }

            action(source);
            return source;
        }
        public static TInput DoFirst<TInput>(this IEnumerable<TInput> source, Action<TInput> action)
          where TInput : class {
            if (!source.Any()) {
                return null;
            }
            var first = source.First();
            action(first);
            return first;
        }
        public static TInput Do<TInput>(this TInput source, Action<TInput> action)
            where TInput : class {
            if (source == null) {
                return null;
            }

            action(source);
            return source;
        }

        public static TInput If<TInput>(this TInput source, Predicate<TInput> predicate)
            where TInput : class {
            if (source == null) {
                return null;
            }

            return predicate(source) ? source : null;
        }

        public static TReturn Return<TInput, TReturn>(this TInput source,
                                                       Func<TInput, TReturn> getter,
                                                      TReturn def = default(TReturn)) where TInput : class {
            if (source == null) {
                return def;
            }

            return getter(source);
        }

        public static TReturn Return<TInput, TReturn>(this TInput source,
                                                       Func<TInput, TReturn> getter,
                                                      Func<TReturn> defaultFactory) where TInput : class {
            if (source == null) {
                return defaultFactory();
            }

            return getter(source);
        }
    }
}
