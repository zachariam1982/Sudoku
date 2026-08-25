#include "pch-cpp.hpp"






struct Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C;
struct BitHelper_t2BEA51BB52EB1672DBF4163ED6757DCEEB3A4DF1;
struct Int32_t680FF22E76F6EFAD4375103CBBFFA0421349384C;
struct String_t;


struct Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C;

IL2CPP_EXTERN_C_BEGIN
IL2CPP_EXTERN_C_END

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
struct BitHelper_t2BEA51BB52EB1672DBF4163ED6757DCEEB3A4DF1  : public RuntimeObject
{
	int32_t ____length;
	int32_t* ____arrayPtr;
	Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* ____array;
	bool ____useStackAlloc;
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F  : public RuntimeObject
{
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F_marshaled_pinvoke
{
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F_marshaled_com
{
};
struct Boolean_t09A6377A54BE2F9E6985A8149F19234FD7DDFE22 
{
	bool ___m_value;
};
struct Int32_t680FF22E76F6EFAD4375103CBBFFA0421349384C 
{
	int32_t ___m_value;
};
struct Void_t4861ACF8F4594C3437BB48B6E56783494B843915 
{
	union
	{
		struct
		{
		};
		uint8_t Void_t4861ACF8F4594C3437BB48B6E56783494B843915__padding[1];
	};
};
struct Boolean_t09A6377A54BE2F9E6985A8149F19234FD7DDFE22_StaticFields
{
	String_t* ___TrueString;
	String_t* ___FalseString;
};
#ifdef __clang__
#pragma clang diagnostic pop
#endif
struct Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C  : public RuntimeArray
{
	ALIGN_FIELD (8) int32_t m_Items[1];

	inline int32_t GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline int32_t* GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, int32_t value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
	}
	inline int32_t GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline int32_t* GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, int32_t value)
	{
		m_Items[index] = value;
	}
};



IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2 (RuntimeObject* __this, const RuntimeMethod* method) ;
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
// Method Definition Index: 68142
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void BitHelper__ctor_m141F24AE9FFCB3EA3D2C7EA79BDAC722026EDEB3 (BitHelper_t2BEA51BB52EB1672DBF4163ED6757DCEEB3A4DF1* __this, int32_t* ___0_bitArrayPtr, int32_t ___1_length, const RuntimeMethod* method) 
{
	{
		Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2(__this, NULL);
		int32_t* L_0 = ___0_bitArrayPtr;
		__this->____arrayPtr = L_0;
		int32_t L_1 = ___1_length;
		__this->____length = L_1;
		__this->____useStackAlloc = (bool)1;
		return;
	}
}
// Method Definition Index: 68143
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void BitHelper__ctor_m795A92E9A03F57547FD78A8E50F730C2778DDD19 (BitHelper_t2BEA51BB52EB1672DBF4163ED6757DCEEB3A4DF1* __this, Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* ___0_bitArray, int32_t ___1_length, const RuntimeMethod* method) 
{
	{
		Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2(__this, NULL);
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_0 = ___0_bitArray;
		__this->____array = L_0;
		Il2CppCodeGenWriteBarrier((void**)(&__this->____array), (void*)L_0);
		int32_t L_1 = ___1_length;
		__this->____length = L_1;
		return;
	}
}
// Method Definition Index: 68144
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void BitHelper_MarkBit_m12EFF71C5444F4E4D076F514C0C0723B39E50F86 (BitHelper_t2BEA51BB52EB1672DBF4163ED6757DCEEB3A4DF1* __this, int32_t ___0_bitPosition, const RuntimeMethod* method) 
{
	int32_t V_0 = 0;
	int32_t V_1 = 0;
	{
		int32_t L_0 = ___0_bitPosition;
		V_0 = ((int32_t)(L_0/((int32_t)32)));
		int32_t L_1 = V_0;
		int32_t L_2 = __this->____length;
		if ((((int32_t)L_1) >= ((int32_t)L_2)))
		{
			goto IL_0046;
		}
	}
	{
		int32_t L_3 = V_0;
		if ((((int32_t)L_3) < ((int32_t)0)))
		{
			goto IL_0046;
		}
	}
	{
		int32_t L_4 = ___0_bitPosition;
		V_1 = ((int32_t)(1<<((int32_t)(((int32_t)(L_4%((int32_t)32)))&((int32_t)31)))));
		bool L_5 = __this->____useStackAlloc;
		if (!L_5)
		{
			goto IL_0035;
		}
	}
	{
		int32_t* L_6 = __this->____arrayPtr;
		int32_t L_7 = V_0;
		int32_t* L_8 = ((int32_t*)il2cpp_codegen_add((intptr_t)L_6, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)L_7), 4))));
		int32_t L_9 = (*(L_8));
		int32_t L_10 = V_1;
		*((int32_t*)L_8) = (int32_t)((int32_t)(L_9|L_10));
		return;
	}

IL_0035:
	{
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_11 = __this->____array;
		int32_t L_12 = V_0;
		NullCheck(L_11);
		int32_t* L_13 = ((L_11)->GetAddressAt(static_cast<il2cpp_array_size_t>(L_12)));
		int32_t L_14 = *((int32_t*)L_13);
		int32_t L_15 = V_1;
		*((int32_t*)L_13) = (int32_t)((int32_t)(L_14|L_15));
	}

IL_0046:
	{
		return;
	}
}
// Method Definition Index: 68145
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool BitHelper_IsMarked_m0A02826959B4EF6381B8F6C7DF0EDBFC55EE8EF2 (BitHelper_t2BEA51BB52EB1672DBF4163ED6757DCEEB3A4DF1* __this, int32_t ___0_bitPosition, const RuntimeMethod* method) 
{
	int32_t V_0 = 0;
	int32_t V_1 = 0;
	{
		int32_t L_0 = ___0_bitPosition;
		V_0 = ((int32_t)(L_0/((int32_t)32)));
		int32_t L_1 = V_0;
		int32_t L_2 = __this->____length;
		if ((((int32_t)L_1) >= ((int32_t)L_2)))
		{
			goto IL_0044;
		}
	}
	{
		int32_t L_3 = V_0;
		if ((((int32_t)L_3) < ((int32_t)0)))
		{
			goto IL_0044;
		}
	}
	{
		int32_t L_4 = ___0_bitPosition;
		V_1 = ((int32_t)(1<<((int32_t)(((int32_t)(L_4%((int32_t)32)))&((int32_t)31)))));
		bool L_5 = __this->____useStackAlloc;
		if (!L_5)
		{
			goto IL_0036;
		}
	}
	{
		int32_t* L_6 = __this->____arrayPtr;
		int32_t L_7 = V_0;
		int32_t L_8 = (*(((int32_t*)il2cpp_codegen_add((intptr_t)L_6, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)L_7), 4))))));
		int32_t L_9 = V_1;
		return (bool)((!(((uint32_t)((int32_t)(L_8&L_9))) <= ((uint32_t)0)))? 1 : 0);
	}

IL_0036:
	{
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_10 = __this->____array;
		int32_t L_11 = V_0;
		NullCheck(L_10);
		int32_t L_12 = L_11;
		int32_t L_13 = (L_10)->GetAt(static_cast<il2cpp_array_size_t>(L_12));
		int32_t L_14 = V_1;
		return (bool)((!(((uint32_t)((int32_t)(L_13&L_14))) <= ((uint32_t)0)))? 1 : 0);
	}

IL_0044:
	{
		return (bool)0;
	}
}
// Method Definition Index: 68146
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t BitHelper_ToIntArrayLength_m59204C3775D26A8B9532246C2F384C92D02E713C (int32_t ___0_n, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = ___0_n;
		if ((((int32_t)L_0) > ((int32_t)0)))
		{
			goto IL_0006;
		}
	}
	{
		return 0;
	}

IL_0006:
	{
		int32_t L_1 = ___0_n;
		return ((int32_t)il2cpp_codegen_add(((int32_t)(((int32_t)il2cpp_codegen_subtract(L_1, 1))/((int32_t)32))), 1));
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
