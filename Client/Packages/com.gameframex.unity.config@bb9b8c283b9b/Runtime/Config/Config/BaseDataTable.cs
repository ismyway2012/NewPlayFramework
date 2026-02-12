// ==========================================================================================
//  GameFrameX 组织及其衍生项目的版权、商标、专利及其他相关权利
//  GameFrameX organization and its derivative projects' copyrights, trademarks, patents, and related rights
//  均受中华人民共和国及相关国际法律法规保护。
//  are protected by the laws of the People's Republic of China and relevant international regulations.
// 
//  使用本项目须严格遵守相应法律法规及开源许可证之规定。
//  Usage of this project must strictly comply with applicable laws, regulations, and open-source licenses.
// 
//  本项目采用 MIT 许可证与 Apache License 2.0 双许可证分发，
//  This project is dual-licensed under the MIT License and Apache License 2.0,
//  完整许可证文本请参见源代码根目录下的 LICENSE 文件。
//  please refer to the LICENSE file in the root directory of the source code for the full license text.
// 
//  禁止利用本项目实施任何危害国家安全、破坏社会秩序、
//  It is prohibited to use this project to engage in any activities that endanger national security, disrupt social order,
//  侵犯他人合法权益等法律法规所禁止的行为！
//  or infringe upon the legitimate rights and interests of others, as prohibited by laws and regulations!
//  因基于本项目二次开发所产生的一切法律纠纷与责任，
//  Any legal disputes and liabilities arising from secondary development based on this project
//  本项目组织与贡献者概不承担。
//  shall be borne solely by the developer; the project organization and contributors assume no responsibility.
// 
//  GitHub 仓库：https://github.com/GameFrameX
//  GitHub Repository: https://github.com/GameFrameX
//  Gitee  仓库：https://gitee.com/GameFrameX
//  Gitee Repository:  https://gitee.com/GameFrameX
//  官方文档：https://gameframex.doc.alianblank.com/
//  Official Documentation: https://gameframex.doc.alianblank.com/
// ==========================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Scripting;

namespace GameFrameX.Config.Runtime
{
    [Preserve]
    public abstract class BaseDataTable<T> : IDataTable<T> where T : class
    {
        protected readonly SortedDictionary<long, T> LongDataMaps = new SortedDictionary<long, T>();
        protected readonly SortedDictionary<string, T> StringDataMaps = new SortedDictionary<string, T>();

        protected readonly List<T> DataList = new List<T>();
        public abstract Task LoadAsync();

        public T Get(int id)
        {
            LongDataMaps.TryGetValue(id, out T value);
            return value;
        }

        public T Get(long id)
        {
            LongDataMaps.TryGetValue(id, out T value);
            return value;
        }

        public T Get(string id)
        {
            StringDataMaps.TryGetValue(id, out T value);
            return value;
        }

        public bool TryGet(int id, out T value)
        {
            return LongDataMaps.TryGetValue(id, out value);
        }

        public bool TryGet(long id, out T value)
        {
            return LongDataMaps.TryGetValue(id, out value);
        }

        public bool TryGet(string id, out T value)
        {
            return StringDataMaps.TryGetValue(id, out value);
        }

        public T this[int index]
        {
            get
            {
                if (index >= Count || index < 0)
                {
                    throw new IndexOutOfRangeException(nameof(index));
                }

                return DataList[index];
            }
        }

        public T this[long id]
        {
            get { return Get(id); }
        }

        public T this[string index]
        {
            get { return Get(index); }
        }

        public int Count
        {
            get { return Math.Max(LongDataMaps.Count, StringDataMaps.Count); }
        }

        public T FirstOrDefault
        {
            get { return DataList.FirstOrDefault(); }
        }

        public T LastOrDefault
        {
            get { return DataList.LastOrDefault(); }
        }

        public T[] All
        {
            get { return DataList.ToArray(); }
        }

        public T[] ToArray()
        {
            return DataList.ToArray();
        }

        public List<T> ToList()
        {
            return DataList.ToList();
        }

        public T Find(Func<T, bool> func)
        {
            return DataList.FirstOrDefault(func);
        }

        public T[] FindListArray(Func<T, bool> func)
        {
            return DataList.Where(func).ToArray();
        }

        public List<T> FindList(Func<T, bool> func)
        {
            return DataList.Where(func).ToList();
        }

        public void ForEach(Action<T> func)
        {
            DataList.ForEach(func);
        }

        public Tk Max<Tk>(Func<T, Tk> func) where Tk : IComparable<Tk>
        {
            return DataList.Max(func);
        }

        public Tk Min<Tk>(Func<T, Tk> func) where Tk : IComparable<Tk>
        {
            return DataList.Min(func);
        }

        public int Sum(Func<T, int> func)
        {
            return DataList.Sum(func);
        }

        public long Sum(Func<T, long> func)
        {
            return DataList.Sum(func);
        }

        public float Sum(Func<T, float> func)
        {
            return DataList.Sum(func);
        }

        public double Sum(Func<T, double> func)
        {
            return DataList.Sum(func);
        }

        public decimal Sum(Func<T, decimal> func)
        {
            return DataList.Sum(func);
        }
    }
}