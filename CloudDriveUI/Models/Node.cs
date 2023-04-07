﻿using System.Collections;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CloudDriveUI.Models;

public class Node<T> : IEnumerable<Node<T>>
{
    /// <summary>
    /// 当前节点名
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// 当前节点值
    /// </summary>
    public T? Value { get; set; }
    /// <summary>
    /// 子节点
    /// </summary>
    public List<Node<T>> Children { get; set; } = new();
    /// <summary>
    /// 父节点
    /// </summary>
    public Node<T>? Parent { get; set; }

    /// <summary>
    /// 索引器
    /// </summary>
    /// <param name="path">子节点路径</param>
    /// <returns></returns>
    /// <exception cref="IndexOutOfRangeException">索引路径错误</exception>
    public Node<T> this[string path]
    {
        get
        {
            var res = Children.Find(node => node.Name == path);
            if (res == null) throw new IndexOutOfRangeException($"不存在索引为{path}的子节点");
            return res;
        }
        set
        {
            var res = Children.Find(node => node.Name == path);
            if (res == null) throw new IndexOutOfRangeException($"不存在索引为{path}的子节点");
            res = value;
        }
    }

    /// <summary>
    /// 路径
    /// </summary>
    public string Path
    {
        get
        {
            return $"{Parent?.Path ?? ""}\\{Name}";
        }
    }

    public Node(string name)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentException($"参数不能为空字符");
        Name = name;
    }

    public Node(string name, T? value) : this(name)
    {
        Value = value;
    }

    public Node(string name, Node<T> parent) : this(name)
    {
        Parent = parent;
    }
    public Node(string name, T value, Node<T> parent) : this(name, value)
    {
        Parent = parent;
    }

    /// <summary>
    /// 获取子节点
    /// </summary>
    /// <param name="paths">节点路径</param>
    /// <exception cref="IndexOutOfRangeException">索引路径错误</exception>
    public Node<T> GetNode(string[] paths)
    {
        if (paths.Length == 0) return this;
        var child = this[paths[0]];
        return child.GetNode(paths[1..]);
    }
    public Node<T> GetNode(string path, char path_separator = '\\')
    {
        if (string.IsNullOrEmpty(path)) return this;
        var paths = path.Trim(path_separator).Split(path_separator).ToArray();
        return GetNode(paths);
    }


    /// <summary>
    /// 获取节点值
    /// </summary>
    /// <param name="result">结果</param>
    /// <param name="paths">路径</param>
    /// <returns></returns>
    public bool TryGetValue(out T? result, string[] paths)
    {
        result = default;
        if (paths.Length == 0)
        {
            result = Value;
            return true;
        }
        var child = Children.Find(e => e.Name == paths[0]);
        if (child == null) return false;
        else return child.TryGetValue(out result, paths[1..]);
    }
    /// <summary>
    /// 获取节点值
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="result">结果</param>
    /// <param name="path_separator">路径分隔符</param>
    /// <returns></returns>
    public bool TryGetValue(out T? result,string path,  char path_separator = '\\')
    {
        var paths = path.Trim(path_separator).Split(path_separator).ToArray();
        return TryGetValue(out result, paths);
    }

    /// <summary>
    /// 设置节点值
    /// </summary>
    /// <param name="value">节点值</param>
    /// <param name="paths">定位路径</param>
    public bool TrySetValue(T? value, params string[] paths)
    {
        if (paths.Length == 0)
        {
            Value = value;
            return true;
        }
        var child = Children.Find(e => e.Name == paths[0]);
        if (child == null) return false;
        else return child.TrySetValue(value, paths[1..]);

    }
    /// <summary>
    /// 获取节点值
    /// </summary>
    /// <param name="value">节点值</param>
    /// <param name="path">路径</param>
    /// <param name="path_separator">路径分隔符</param>
    /// <returns></returns>
    public bool TrySetValue(T? value, string path, char path_separator = '\\')
    {
        var paths = path.Trim(path_separator).Split(path_separator).ToArray();
        return TrySetValue(value, paths);
    }


    /// <summary>
    /// 插入节点
    /// </summary>
    /// <param name="node">待插入节点</param>
    /// <param name="paths">插入位置，当前节点的相对路径</param>
    /// <exception cref="ArgumentNullException">路径参数不能为空或空字符</exception>
    public void Insert(Node<T> node, params string[] paths)
    {
        if (paths.Any(string.IsNullOrEmpty)) throw new ArgumentNullException("路径参数不能为空或空字符");
        // 插入到当前节点,递归终点
        if (paths.Length == 0)
        {
            // 如果已包含该子节点且节点值不为空，报错，否则替换该子节点值
            var tmp = Children.Find(n => n.Name == node.Name);
            Console.WriteLine(tmp);
            if (tmp == null)
            {
                node.Parent = this;
                Children.Add(node);
            }
            else if (tmp.Value == null) tmp.Value = node.Value;
            else throw new ArgumentException($"{node.Name}节点已经存在");
        }
        else
        {
            var child = Children.Find(e => e.Name == paths[0]);
            // 递归调用
            if (child != null)
            {
                var newNode = new Node<T>(paths[0], node.Value);
                child.Insert(newNode, paths[1..]);
            }
            // 如果路径没有找到,终结递归调用
            else
            {
                var newNode = Node<T>.FromPaths(paths);
                newNode.Parent = this;
                newNode.TrySetValue(node.Value, paths[1..]);
                Children.Add(newNode);
            }
        }
    }
    /// <summary>
    /// 根据路径字符串插入节点
    /// </summary>
    /// <param name="node">待插入节点</param>
    /// <param name="path">插入到</param>
    /// <param name="path_separator">路径分隔符</param>
    public void Insert(Node<T> node, string path, char path_separator = '\\')
    {
        var paths = path.Split(path_separator).Where(e => !string.IsNullOrEmpty(e)).ToArray();
        Insert(node, paths);
    }

    /// <summary>
    /// 从路径创建空值节点
    /// </summary>
    /// <param name="paths">节点路径，同时作为节点名称</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">paths中包含空字符</exception>
    /// <exception cref="ArgumentException">paths参数长度不能为0</exception>
    public static Node<T> FromPaths(string[] paths)
    {
        if (paths.Any(string.IsNullOrEmpty)) throw new ArgumentNullException("路径参数不能为空或空字符");
        if (paths.Length == 0) throw new ArgumentException("参数长度必须大于0");
        var root = new Node<T>(paths[0]);
        if (paths.Length > 1)
        {
            var tmp = root;
            foreach (var name in paths[1..])
            {
                var newNode = new Node<T>(name, tmp);
                tmp.Children.Add(newNode);
                tmp = newNode;
            }
        }
        return root;
    }

    /// <summary>
    /// 从路径创建空值节点
    /// </summary>
    /// <param name="paths">节点路径</param>
    /// <param name="path_separator">路径分隔符</param>
    /// <returns></returns>
    public static Node<T> FromPaths(string path, char path_separator = '\\')
    {
        var paths = path.Split(path_separator).Where(e => !string.IsNullOrEmpty(e)).ToArray();
        return FromPaths(paths);
    }


    /// <summary>
    /// 遍历打印节点
    /// </summary>
    public void PrintAll(int indent = 0)
    {
        Console.WriteLine($"{new string(' ', indent)}{Name}");
        foreach (var node in Children)
        {
            node.PrintAll(indent + 4);
        }
    }

    /// <summary>
    /// 广度优先遍历
    /// </summary>
    /// <returns></returns>
    public IEnumerator<Node<T>> BFS()
    {
        var queue = new Queue<IEnumerator<Node<T>>>();
        yield return this;
        queue.Enqueue(Children.GetEnumerator());
        while (queue.Count > 0)
        {
            var enumerator = queue.Dequeue();
            while (enumerator.MoveNext())
            {
                var cur = enumerator.Current;
                queue.Enqueue(cur.Children.GetEnumerator());
                yield return cur;
            }
        }
    }

    /// <summary>
    /// 深度优先遍历
    /// </summary>
    /// <returns></returns>
    public IEnumerator<Node<T>> DFS()
    {
        var stack = new Stack<IEnumerator<Node<T>>>();
        yield return this;
        stack.Push(Children.GetEnumerator());
        while (stack.Count > 0)
        {
            var enumerator = stack.Pop();
            while (enumerator.MoveNext())
            {
                var cur = enumerator.Current;
                stack.Push(cur.Children.GetEnumerator());
                yield return cur;
            }
        }
    }

    public IEnumerator<Node<T>> GetEnumerator()
    {
        return BFS();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

}

