/* The MIT License (MIT)
* 
* Copyright (c) 2015 Marc Clifton
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Clifton.Core.ExtensionMethods
{
	public static class ExtensionMethods
	{
		public static bool If<T>(this T v, Func<T, bool> predicate, Action<T> action)
		{
			bool ret = predicate(v);

			if (ret)
			{
				action(v);
			}

			return ret;
		}

		public static bool If(this bool b, Action action)
		{
			if (b)
			{
				action();
			}

			return b;
		}

		public static void IfElse(this bool b, Action ifTrue, Action ifFalse)
		{
			if (b) ifTrue(); else ifFalse();
		}

		// Type is...
		public static bool Is<T>(this object obj, Action<T> action)
		{
			bool ret = obj is T;

			if (ret)
			{
				action((T)obj);
			}

			return ret;
		}

		// ---------- if-then-else as lambda expressions --------------

		// If the test returns true, execute the action.
		// Works with objects, not value types.
		public static void IfTrue<T>(this T obj, Func<T, bool> test, Action<T> action)
		{
			if (test(obj))
			{
				action(obj);
			}
		}

		/// <summary>
		/// Returns true if the object is null.
		/// </summary>
		public static bool IfNull<T>(this T obj)
		{
			return obj == null;
		}

		/// <summary>
		/// If the object is null, performs the action and returns true.
		/// </summary>
		public static bool IfNull<T>(this T obj, Action action)
		{
			bool ret = obj == null;

			if (ret) { action(); }

			return ret;
		}

		/// <summary>
		/// Returns true if the object is not null.
		/// </summary>
		public static bool IfNotNull<T>(this T obj)
		{
			return obj != null;
		}

		/// <summary>
		/// Return the result of the func if 'T is not null, passing 'T to func.
		/// </summary>
		public static R IfNotNullReturn<T, R>(this T obj, Func<T, R> func)
		{
			if (obj != null)
			{
				return func(obj);
			}
			else
			{
				return default(R);
			}
		}

		/// <summary>
		/// Return the result of func if 'T is null.
		/// </summary>
		public static R ElseIfNullReturn<T, R>(this T obj, Func<R> func)
		{
			if (obj == null)
			{
				return func();
			}
			else
			{
				return default(R);
			}
		}

		/// <summary>
		/// If the object is not null, performs the action and returns true.
		/// </summary>
		public static bool IfNotNull<T>(this T obj, Action<T> action)
		{
			bool ret = obj != null;

			if (ret) { action(obj); }

			return ret;
		}

		/// <summary>
		/// If not null, return the evaluation of the function, otherwise return the default value.
		/// </summary>
		public static R IfNotNull<T, R>(this T obj, R defaultValue, Func<T, R> func)
		{
			R ret = defaultValue;

			if (obj != null)
			{
				ret = func(obj);
			}

			return ret;
		}

		/// <summary>
		/// If the boolean is true, performs the specified action.
		/// </summary>
		public static bool Then(this bool b, Action f)
		{
			if (b) { f(); }

			return b;
		}

		/// <summary>
		/// If the boolean is false, performs the specified action and returns the complement of the original state.
		/// </summary>
		public static void Else(this bool b, Action f)
		{
			if (!b) { f(); }
		}

		// ---------- Dictionary --------------

		/// <summary>
		/// Return the key for the dictionary value or throws an exception if more than one value matches.
		/// </summary>
		public static TKey KeyFromValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, TValue val)
		{
			// from: http://stackoverflow.com/questions/390900/cant-operator-be-applied-to-generic-types-in-c
			// "Instead of calling Equals, it's better to use an IComparer<T> - and if you have no more information, EqualityComparer<T>.Default is a good choice: Aside from anything else, this avoids boxing/casting."
			return dict.Single(t => EqualityComparer<TValue>.Default.Equals(t.Value, val)).Key;
		}

		// ---------- DBNull value --------------

		// Note the "where" constraint, only value types can be used as Nullable<T> types.
		// Otherwise, we get a bizzare error that doesn't really make it clear that T needs to be restricted as a value type.
		public static object AsDBNull<T>(this Nullable<T> item) where T : struct
		{
			// If the item is null, return DBNull.Value, otherwise return the item.
			return item as object ?? DBNull.Value;
		}

		// ---------- ForEach iterators --------------

		/// <summary>
		/// Implements a ForEach for generic enumerators.
		/// </summary>
		public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
		{
			foreach (var item in collection)
			{
				action(item);
			}
		}

		/// <summary>
		/// ForEach with an index.
		/// </summary>
		public static void ForEachWithIndex<T>(this IEnumerable<T> collection, Action<T, int> action)
		{
			int n = 0;

			foreach (var item in collection)
			{
				action(item, n++);
			}
		}

		/// <summary>
		/// Implements ForEach for non-generic enumerators.
		/// </summary>
		// Usage: Controls.ForEach<Control>(t=>t.DoSomething());
		public static void ForEach<T>(this IEnumerable collection, Action<T> action)
		{
			foreach (T item in collection)
			{
				action(item);
			}
		}

		public static T Single<T>(this IEnumerable collection, Func<T, bool> expr)
		{
			T ret = default(T);
			bool found = false;

			foreach (T item in collection)
			{
				if (expr(item))
				{
					ret = item;
					found = true;
					break;
				}
			}

			found.Else(() => { throw new ApplicationException("Collection does not contain item in qualifier."); });

			return ret;
		}

		public static bool Contains<T>(this IEnumerable collection, Func<T, bool> expr)
		{
			bool found = false;

			foreach (T item in collection)
			{
				if (expr(item))
				{
					found = true;
					break;
				}
			}

			return found;
		}

		public static T SingleOrDefault<T>(this IEnumerable collection, Func<T, bool> expr)
		{
			T ret = default(T);

			foreach (T item in collection)
			{
				if (expr(item))
				{
					ret = item;
					break;
				}
			}

			return ret;
		}

		public static void ForEach(this DataTable dt, Action<DataRow> action)
		{
			foreach (DataRow row in dt.Rows)
			{
				action(row);
			}
		}

		public static void ForEach(this DataView dv, Action<DataRowView> action)
		{
			foreach (DataRowView drv in dv)
			{
				action(drv);
			}
		}

		/// <summary>
		/// Returns a new dictionary having merged the two source dictionaries.
		/// </summary>
		public static Dictionary<T, U> Merge<T, U>(this Dictionary<T, U> dict1, Dictionary<T, U> dict2)
		{
			return (new[] { dict1, dict2 }).SelectMany(dict => dict).ToDictionary(pair => pair.Key, pair => pair.Value);
		}

		// ---------- events --------------

		/// <summary>
		/// Encapsulates testing for whether the event has been wired up.
		/// </summary>
		public static void Fire<TEventArgs>(this EventHandler<TEventArgs> theEvent, object sender, TEventArgs e) where TEventArgs : EventArgs
		{
			if (theEvent != null)
			{
				theEvent(sender, e);
			}
		}

		/// <summary>
		/// Encapsulates testing for whether the event has been wired up.
		/// </summary>
		public static void Fire<TEventArgs>(this PropertyChangedEventHandler theEvent, object sender, TEventArgs e) where TEventArgs : PropertyChangedEventArgs
		{
			if (theEvent != null)
			{
				theEvent(sender, e);
			}
		}

		// ---------- collection management --------------

		// From the comments of the blog entry http://blog.jordanterrell.com/post/LINQ-Distinct()-does-not-work-as-expected.aspx regarding why Distinct doesn't work right.
		public static IEnumerable<T> RemoveDuplicates<T>(this IEnumerable<T> source)
		{
			return RemoveDuplicates(source, (t1, t2) => t1.Equals(t2));
		}

		public static IEnumerable<T> RemoveDuplicates<T>(this IEnumerable<T> source, Func<T, T, bool> equater)
		{
			// copy the source array 
			List<T> result = new List<T>();

			foreach (T item in source)
			{
				if (result.All(t => !equater(item, t)))
				{
					// Doesn't exist already: Add it 
					result.Add(item);
				}
			}

			return result;
		}

		public static IEnumerable<T> Replace<T>(this IEnumerable<T> source, T newItem, Func<T, T, bool> equater)
		{
			List<T> result = new List<T>();

			foreach (T item in source)
			{
				if (!equater(item, newItem))
				{
					result.Add(item);
				}
			}

			result.Add(newItem);

			return result;
		}

		public static void AddIfUnique<T>(this IList<T> list, T item)
		{
			if (!list.Contains(item))
			{
				list.Add(item);
			}
		}

		public static void AddIfUnique<T>(this IList<T> list, T item, Func<T, bool> comparer)
		{
			if (!list.Contains(comparer))
			{
				list.Add(item);
			}
		}

		public static void RemoveLast<T>(this IList<T> list)
		{
			list.RemoveAt(list.Count - 1);
		}

		// ---------- List to DataTable --------------

		// From http://stackoverflow.com/questions/564366/generic-list-to-datatable
		// which also suggests, for better performance, HyperDescriptor: http://www.codeproject.com/Articles/18450/HyperDescriptor-Accelerated-dynamic-property-acces
		public static DataTable AsDataTable<T>(this IList<T> data)
		{
			PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(T));
			DataTable table = new DataTable();

			for (int i = 0; i < props.Count; i++)
			{
				PropertyDescriptor prop = props[i];
				table.Columns.Add(prop.Name, prop.PropertyType);
			}

			object[] values = new object[props.Count];

			foreach (T item in data)
			{
				for (int i = 0; i < values.Length; i++)
				{
					values[i] = props[i].GetValue(item);
				}
				table.Rows.Add(values);
			}

			return table;
		}

		public static bool IsEmpty(this string s)
		{
			return s == String.Empty;
		}
#if INCLUDE_DRAWING
		public static Image Resize(this Image image, int maxWidth)
		{
			// To encode as JPG, see http://stackoverflow.com/questions/10894836/c-sharp-convert-image-formats-to-jpg
			// scale the height to the desired width.
			int height = image.Height * maxWidth / image.Width;
			return (Image)(new Bitmap(image, new Size(maxWidth, height)));

			// High quality conversion.  see http://stackoverflow.com/questions/1922040/resize-an-image-c-sharp
			// But CompositingMode isn't resolving even though I have .NET 4.5 selected.
			/*
			Rectangle destRect = new Rectangle(0, 0, width, height);
			Image destImage = new Bitmap(width, height);

			destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

			using (var graphics = Graphics.FromImage(destImage))
			{
				graphics.CompositingMode = CompositingMode.SourceCopy;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

				using (var wrapMode = new ImageAttributes())
				{
					wrapMode.SetWrapMode(WrapMode.TileFlipXY);
					graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
				}
			}
			 */
		}

		public static string ToBase64(this Image image)
		{
			string base64String = String.Empty;

			if (image != null)
			{
				// For some reason, the RawFormat converter sometimes throws an exception, so we are saving to a file.
				image.Save("id2.jpg", ImageFormat.Jpeg);
				byte[] data = File.ReadAllBytes("id2.jpg");
				base64String = Convert.ToBase64String(data);

				/*
				using (MemoryStream m = new MemoryStream())
				{
					image.Save(m, image.RawFormat);
					byte[] imageBytes = m.ToArray();

					// Convert byte[] to Base64 String
					base64String = Convert.ToBase64String(imageBytes);
				}
				 */
			}

			return base64String;
		}

		/// Returns just the RGB component of a color.
		public static int ToRgb(this Color color)
		{
			return color.ToArgb() & 0x00FFFFFF;
		}

#endif

		/// <summary>
		/// Return the smaller of the two values.
		/// </summary>
		public static int Smaller(this int val, int otherVal)
		{
			return val < otherVal ? val : otherVal;
		}

		/// <summary>
		/// Returns the larger of the two values.
		/// </summary>
		public static int Larger(this int val, int otherVal)
		{
			return val > otherVal ? val : otherVal;
		}

		public static double ToPosix(this DateTime date)
		{
			DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

			return (date - start).TotalSeconds;
		}

		public static DateTime FromPosix(this double posix)
		{
			DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

			return start.AddSeconds(posix).ToLocalTime();
		}

		public static string DateFromPosix(this string s, string format = "MM-dd-yy")
		{
			DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			dt = dt.AddSeconds(s.to_l());

			return dt.ToString(format);
		}

		public static string DateFromPosix(this long l, string format = "MM-dd-yy")
		{
			DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			dt = dt.AddSeconds(l);

			return dt.ToString(format);
		}

		public static string to_s(this int i)
		{
			return i.ToString();
		}

		public static long to_l(this string src)
		{
			long ret = 0;

			if (!String.IsNullOrEmpty(src))
			{
				ret = Convert.ToInt64(src);
			}

			return ret;
		}

		public static int to_i(this string src)
		{
			return Convert.ToInt32(src);
		}

		public static bool to_b(this string src)
		{
			return Convert.ToBoolean(src);
		}

		public static float to_f(this string src)
		{
			return (float)Convert.ToDouble(src);
		}

		public static double to_d(this string src)
		{
			return Convert.ToDouble(src);
		}

		public static decimal to_dec(this string src)
		{
			return Convert.ToDecimal(src);
		}

		public static T ToEnum<T>(this string src)
		{
			T enumVal = (T)Enum.Parse(typeof(T), src);

			return enumVal;
		}

		public static string SafeToString(this Object src)
		{
			string ret = String.Empty;

			if (src != null)
			{
				ret = src.ToString();
			}

			return ret;
		}

		public static bool IsInt32(this String src)
		{
			int result;
			bool ret = Int32.TryParse(src, out result);

			return ret;
		}

		/// <summary>
		/// Replaces quote with single quote.
		/// </summary>
		public static string ParseQuote(this String src)
		{
			return src.Replace("\"", "'");
		}

		/// <summary>
		/// Replaces single quote with two single quotes.
		/// </summary>
		public static string ParseSingleQuote(this String src)
		{
			return src.Replace("'", "''");
		}

		/// <summary>
		/// Returns a new string surrounded by single quotes.
		/// </summary>
		public static string SingleQuote(this String src)
		{
			return "'" + src + "'";
		}

		/// <summary>
		/// Returns a new string surrounded by quotes.
		/// </summary>
		public static string Quote(this String src)
		{
			return "\"" + src + "\"";
		}

		/// <summary>
		/// Exchanges ' for " and " for '
		/// Javascript JSON support, which must be formatted like '{"foo":"bar"}'
		/// </summary>
		public static string ExchangeQuoteSingleQuote(this String src)
		{
			string ret = src.Replace("'", "\0xFF");
			ret = ret.Replace("\"", "'");
			ret = ret.Replace("\0xFF", "\"");

			return ret;
		}

		/// <summary>
		/// Returns the source string surrounded by a single whitespace.
		/// </summary>
		public static string Spaced(this String src)
		{
			return " " + src + " ";
		}

		/// <summary>
		/// Returns a new string surrounded by brackets.
		/// </summary>
		public static string Parens(this String src)
		{
			return "(" + src + ")";
		}

		/// <summary>
		/// Returns a new string surrounded by brackets.
		/// </summary>
		public static string Brackets(this String src)
		{
			return "[" + src + "]";
		}

		/// <summary>
		/// Returns a new string surrounded by brackets.
		/// </summary>
		public static string CurlyBraces(this String src)
		{
			return "{" + src + "}";
		}

		/// <summary>
		/// Returns everything between the start and end chars, exclusive.
		/// </summary>
		/// <param name="src">The source string.</param>
		/// <param name="start">The first char to find.</param>
		/// <param name="end">The end char to find.</param>
		/// <returns>The string between the start and stop chars, or an empty string if not found.</returns>
		public static string Between(this string src, char start, char end)
		{
			string ret = String.Empty;
			int idxStart = src.IndexOf(start);

			if (idxStart != -1)
			{
				++idxStart;
				int idxEnd = src.IndexOf(end, idxStart);

				if (idxEnd != -1)
				{
					ret = src.Substring(idxStart, idxEnd - idxStart);
				}
			}

			return ret;
		}

		public static string Between(this string src, string start, string end)
		{
			string ret = String.Empty;
			int idxStart = src.IndexOf(start);

			if (idxStart != -1)
			{
				idxStart += start.Length;
				int idxEnd = src.IndexOf(end, idxStart);

				if (idxEnd != -1)
				{
					ret = src.Substring(idxStart, idxEnd - idxStart);
				}
			}

			return ret;
		}

		public static string BetweenEnds(this string src, char start, char end)
		{
			string ret = String.Empty;
			int idxStart = src.IndexOf(start);

			if (idxStart != -1)
			{
				++idxStart;
				int idxEnd = src.LastIndexOf(end);

				if (idxEnd != -1)
				{
					ret = src.Substring(idxStart, idxEnd - idxStart);
				}
			}

			return ret;
		}

		/// <summary>
		/// Returns the number of occurances of "find".
		/// </summary>
		/// <param name="src">The source string.</param>
		/// <param name="find">The search char.</param>
		/// <returns>The # of times the char occurs in the search string.</returns>
		public static int Count(this string src, char find)
		{
			int ret = 0;

			foreach (char s in src)
			{
				if (s == find)
				{
					++ret;
				}
			}

			return ret;
		}

		/// <summary>
		/// Return a new string that is "around" (left of and right of) the specified string.
		/// Only the first occurance is processed.
		/// </summary>
		public static string Surrounding(this String src, string s)
		{
			return src.LeftOf(s) + src.RightOf(s);
		}

		public static string RightOf(this String src, char c)
		{
			string ret = String.Empty;
			int idx = src.IndexOf(c);

			if (idx != -1)
			{
				ret = src.Substring(idx + 1);
			}

			return ret;
		}

		public static bool BeginsWith(this String src, string s)
		{
			return src.StartsWith(s);
		}

		public static string RightOf(this String src, string s)
		{
			string ret = String.Empty;
			int idx = src.IndexOf(s);

			if (idx != -1)
			{
				ret = src.Substring(idx + s.Length);
			}

			return ret;
		}

		/// <summary>
		/// Returns everything to the right of the rightmost char c.
		/// </summary>
		/// <param name="src">The source string.</param>
		/// <param name="c">The seach char.</param>
		/// <returns>Returns everything to the right of the rightmost search char, or an empty string.</returns>
		public static string RightOfRightmostOf(this string src, char c)
		{
			string ret = String.Empty;
			int idx = src.LastIndexOf(c);

			if (idx != -1)
			{
				ret = src.Substring(idx + 1);
			}

			return ret;
		}

		/// <summary>
		/// Left of the first occurance of c
		/// </summary>
		/// <param name="src">The source string.</param>
		/// <param name="c">Return everything to the left of this character.</param>
		/// <returns>String to the left of c, or the entire string.</returns>
		public static string LeftOf(this string src, char c)
		{
			string ret = src;
			int idx = src.IndexOf(c);

			if (idx != -1)
			{
				ret = src.Substring(0, idx);
			}

			return ret;
		}

		public static string LeftOf(this String src, string s)
		{
			string ret = src;
			int idx = src.IndexOf(s);

			if (idx != -1)
			{
				ret = src.Substring(0, idx);
			}

			return ret;
		}

		/// <summary>
		/// Returns null if the string is empty.
		/// </summary>
		public static string NullIfEmpty(this String src)
		{
			return String.IsNullOrEmpty(src) ? null : src;
		}

		/// <summary>
		/// Safe left of n chars.  If string length is less than n, returns string.
		/// </summary>
		public static string LeftOf(this String src, int n)
		{
			return src.Length < n ? src : src.Substring(n);
		}

		public static string LeftOfRightmostOf(this String src, char c)
		{
			string ret = src;
			int idx = src.LastIndexOf(c);

			if (idx != -1)
			{
				ret = src.Substring(0, idx);
			}

			return ret;
		}

		public static string LeftOfRightmostOf(this String src, string s)
		{
			string ret = src;
			int idx = src.IndexOf(s);
			int idx2 = idx;

			while (idx2 != -1)
			{
				idx2 = src.IndexOf(s, idx + s.Length);

				if (idx2 != -1)
				{
					idx = idx2;
				}
			}

			if (idx != -1)
			{
				ret = src.Substring(0, idx);
			}

			return ret;
		}

		public static string RightOfRightmostOf(this String src, string s)
		{
			string ret = src;
			int idx = src.IndexOf(s);
			int idx2 = idx;

			while (idx2 != -1)
			{
				idx2 = src.IndexOf(s, idx + s.Length);

				if (idx2 != -1)
				{
					idx = idx2;
				}
			}

			if (idx != -1)
			{
				ret = src.Substring(idx + s.Length, src.Length - (idx + s.Length));
			}

			return ret;
		}

		public static char Rightmost(this String src)
		{
			char c = '\0';

			if (src.Length > 0)
			{
				c = src[src.Length - 1];
			}

			return c;
		}

		public static string TrimLastChar(this String src)
		{
			string ret = String.Empty;
			int len = src.Length;

			if (len > 0)
			{
				ret = src.Substring(0, len - 1);
			}

			return ret;
		}

		public static bool IsBlank(this string src)
		{
			return String.IsNullOrEmpty(src) || (src.Trim() == String.Empty);
		}

		/// <summary>
		/// Returns the first occurance of any token given the list of tokens.
		/// </summary>
		public static string Contains(this String src, string[] tokens)
		{
			string ret = String.Empty;
			int firstIndex = 9999;

			// Find the index of the first index encountered.
			foreach (string token in tokens)
			{
				int idx = src.IndexOf(token);

				if ((idx != -1) && (idx < firstIndex))
				{
					ret = token;
					firstIndex = idx;
				}
			}

			return ret;
		}

		// http://stackoverflow.com/questions/8868119/find-all-parent-types-both-base-classes-and-interfaces
		public static IEnumerable<Type> GetParentTypes(this Type type)
		{
			// is there any base type?
			if ((type == null) || (type.BaseType == null))
			{
				yield break;
			}

			// return all implemented or inherited interfaces
			foreach (var i in type.GetInterfaces())
			{
				yield return i;
			}

			// return all inherited types
			var currentBaseType = type.BaseType;
			while (currentBaseType != null)
			{
				yield return currentBaseType;
				currentBaseType = currentBaseType.BaseType;
			}
		}

		public static byte[] FromBase64(this char[] data)
		{
			return Convert.FromBase64CharArray(data, 0, data.Length);
		}

		public static byte[] FromBase64(this string data)
		{
			return Convert.FromBase64String(data);
		}

		public static string ToBase64(this byte[] data)
		{
			return Convert.ToBase64String(data);
		}

		public static byte[] to_Utf8(this string str)
		{
			return Encoding.UTF8.GetBytes(str);
		}

		// From here: http://www.codeproject.com/Articles/769741/Csharp-AES-bits-Encryption-Library-with-Salt
		public static byte[] AES_Encrypt(this byte[] bytesToBeEncrypted, byte[] passwordBytes)
		{
			byte[] encryptedBytes = null;

			// Set your salt here, change it to meet your flavor:
			// The salt bytes must be at least 8 bytes.
			byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

			using (MemoryStream ms = new MemoryStream())
			{
				using (RijndaelManaged AES = new RijndaelManaged())
				{
					AES.KeySize = 256;
					AES.BlockSize = 128;

					var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
					AES.Key = key.GetBytes(AES.KeySize / 8);
					AES.IV = key.GetBytes(AES.BlockSize / 8);

					AES.Mode = CipherMode.CBC;

					using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
					{
						cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
						cs.Close();
					}
					encryptedBytes = ms.ToArray();
				}
			}

			return encryptedBytes;
		}

		public static byte[] AES_Decrypt(this byte[] bytesToBeDecrypted, byte[] passwordBytes)
		{
			byte[] decryptedBytes = null;

			// Set your salt here, change it to meet your flavor:
			// The salt bytes must be at least 8 bytes.
			byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

			using (MemoryStream ms = new MemoryStream())
			{
				using (RijndaelManaged AES = new RijndaelManaged())
				{
					AES.KeySize = 256;
					AES.BlockSize = 128;

					var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
					AES.Key = key.GetBytes(AES.KeySize / 8);
					AES.IV = key.GetBytes(AES.BlockSize / 8);

					AES.Mode = CipherMode.CBC;

					using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
					{
						cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
						cs.Close();
					}
					decryptedBytes = ms.ToArray();
				}
			}

			return decryptedBytes;
		}

		public static string SplitCamelCase(this string input)
		{
			// return Regex.Replace(input, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
			// Replaced, because the version below also handles strings like "IBMMakeStuffAndSellIt", converting it to "IBM Make Stuff And Sell It"
			// See http://stackoverflow.com/questions/5796383/insert-spaces-between-words-on-a-camel-cased-token
			return Regex.Replace(
				Regex.Replace(input, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"),
					@"(\p{Ll})(\P{Ll})", "$1 $2");
		}

		public static string CamelCase(this string src)
		{
			return src[0].ToString().ToLower() + src.Substring(1).ToLower();

		}

	        /// <summary>
	        /// Converts the first letter of the given string to uppercase. The rest 
	        /// of the string remains unchanged. [Currently used as a utility for the method 
	        /// PascalCaseWords().]
	        /// </summary>
	        /// <param name="src"></param>
	        /// <returns>string</returns>
		public static string PascalCase(this string src)
		{
			string ret = String.Empty;

			if (!String.IsNullOrEmpty(src))
			{
				ret = src[0].ToString().ToUpper() + src.Substring(1);
			}

			return ret;
		}

	        /// <summary>
	        /// Returns a Pascal-cased string, i.e. the first letter of each word is in 
	        /// uppercase, including the first word. The string is otherwise unchanged; 
	        /// spaces and punctuation are preserved.
	        /// </summary>
	        /// <param name="src"></param>
	        /// <returns>string</returns>
		public static string PascalCaseWords(this string src)
		{
			StringBuilder sb = new StringBuilder();
			string[] s = src.Split(' ');
			string more = String.Empty;

			foreach (string s1 in s)
			{
				sb.Append(more);
				sb.Append(PascalCase(s1));
				more = " ";
			}

			return sb.ToString();
		}

	        /// <summary>
	        /// Returns true or false as to whether the first argument is greater
	        /// than or equal to the second argument, and less than or equal to
	        /// the third argument.
	        /// </summary>
	        /// <remarks>
	        /// The name of this method is a bit misleading. It doesn't test only for
	        /// 'between'.
	        /// </remarks>
	        /// <param name="b">The integer to test.</param>
	        /// <param name="a">The integer that should be less than or equal to the test.</param>
	        /// <param name="c">The integer that should be greater than or equal to the test.</param>
	        /// <returns>bool</returns>
		public static bool Between(this int b, int a, int c)
		{
			return b >= a && b <= c;
		}

	        /// <summary>
	        /// Checks that an integer is not greater than a certain value and changes 
	        /// it if it is. If the integer (the first argument) is greater than the
	        /// maximum allowable (the second argument), it is changed to equal the maximum
	        /// allowable, then returned. If the first argument is less than or equal to 
	        /// the second argument, it is returned unchanged.
	        /// </summary>
	        /// <param name="a"></param>
	        /// <param name="max"></param>
	        /// <returns>int</returns>
		public static int Max(this int a, int max)
		{
			if (a > max)
			{
				a = max;
			}

			return a;
		}

	        /// <summary>
	        /// Checks that an integer is not less than a certain value and changes 
	        /// it if it is. If the integer (the first argument) is less than the 
	        /// minimum allowable (the second argument), it is changed to equal the minimum 
	        /// allowable, then returned. If the first argument is greater than or equal to 
	        /// the second argument, it is returned unchanged.
	        /// </summary>
	        /// <param name="a"></param>
	        /// <param name="min"></param>
	        /// <returns>int</returns>
		public static int Min(this int a, int min)
		{
			if (a < min)
			{
				a = min;
			}

			return a;
		}

		public static void Until(this int start, int max, Action<int> action)
		{
			for (int i = start; i < max; i++) action(i);
		}
	}
}
