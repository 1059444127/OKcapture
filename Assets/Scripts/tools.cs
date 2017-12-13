using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class tools {

    public static T[] RemoveAt<T>(this T[] source, int index)
    {
        try
        {
            T[] dest = new T[source.Length - 1];
            if (index > 0)
                Array.Copy(source, 0, dest, 0, index);

            if (index < source.Length - 1)
                Array.Copy(source, index + 1, dest, index, source.Length - index - 1);

            return dest;
        } catch (Exception e)
        {
            Debug.Log(e.Message);
            throw e;
        }
    }

    public static T[] ChopAt<T>(this T[] source, int index)
    {
        try
        {
            T[] dest = new T[index];
            if (index < source.Length)
                Array.Copy(source, 0, dest, 0, index);

            return dest;
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            throw e;
        }

    }

}
