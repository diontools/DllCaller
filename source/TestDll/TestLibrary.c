#ifdef __cplusplus
#define TESTAPI extern "C" __declspec(dllexport)
#else
#define TESTAPI __declspec(dllexport)
#endif

typedef struct BigStructure
{
    unsigned char bytes[1024];
} BigStructure;

TESTAPI void func_void(void)
{
}

TESTAPI int func_int(void)
{
    return 1234567890;
}

TESTAPI void func_void_int(int i)
{
}

TESTAPI int func_int_int(int i)
{
    return i;
}

TESTAPI int func_bool_int(int i)
{
    return i;
}

TESTAPI int func_int_string(unsigned char* str)
{
    int i;
    for (i = 0;; i++)
    {
        if (str[i] == 0)
        {
            break;
        }
    }

    return i;
}

TESTAPI int func_bool_string_int(unsigned char* str, int xyz)
{
    return 1;
}


TESTAPI int func_int_string_int_char_int(unsigned char* str, int xyz, char c, int val)
{
    return 1;
}


TESTAPI void func_void_int1(int v1)
{
}

TESTAPI void func_void_int2(int v1, int v2)
{
}

TESTAPI void func_void_int3(int v1, int v2, int v3)
{
}

TESTAPI void func_void_int4(int v1, int v2, int v3, int v4)
{
}

TESTAPI void func_void_int5(int v1, int v2, int v3, int v4, int v5)
{
}

TESTAPI void func_void_int6(int v1, int v2, int v3, int v4, int v5, int v6)
{
}

TESTAPI void func_void_int7(int v1, int v2, int v3, int v4, int v5, int v6, int v7)
{
}

TESTAPI void func_void_int8(int v1, int v2, int v3, int v4, int v5, int v6, int v7, int v8)
{
}

TESTAPI void func_void_int9(int v1, int v2, int v3, int v4, int v5, int v6, int v7, int v8, int v9)
{
}

TESTAPI void func_void_int10(int v1, int v2, int v3, int v4, int v5, int v6, int v7, int v8, int v9, int v10)
{
}

TESTAPI void func_void_bigStructure(BigStructure bs)
{
}
