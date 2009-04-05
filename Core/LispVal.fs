﻿#light
namespace Tim.Lisp.Core
open System.Reflection.Emit

type LispVal = 
             | ArgReference of int
             | Atom of string
             | Bool of bool
             | CompiledLambda of MethodBuilder
             | CompiledVariable of LocalBuilder
             | IfPrimitive of LispVal * LispVal * LispVal
             | LambdaPrimitive of string list * LispVal
             | List of LispVal list
             | ListPrimitive of ListOp * LispVal list
             | Number of int
             | String of string
             | UnaryPrimitive of UnaryOp * LispVal
             | VariablePrimitive of string * LispVal