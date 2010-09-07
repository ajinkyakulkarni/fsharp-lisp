﻿#light
namespace Tim.Lisp.Core

open System
open System.Reflection
open System.Reflection.Emit

module Asm =
    open Syntax

    type Asm<'a> =
        {
            OpCode : OpCode
            Operand : obj option
            ResultType : Type
            Stack : 'a list
        }

    let tryParseAsm (refs : Assembly list) (using : Set<string>) (expr : Expr<_>) : Asm<Expr<_>> option =
        let getType (name : string) : Type =
            let inAssembly (ref : Assembly) : Type option = 
                using
                |> Seq.tryPick (fun nspace ->
                    match ref.GetType(nspace + "." + name, false) with
                    | null -> None
                    | t -> Some t)

            match List.tryPick inAssembly refs with
            | Some t -> t
            | None -> failwithf "%s is not a .NET type" name

        let getMethod (name : string) (argTypes : Type list) : MethodInfo =
            let typeName, methodName =
                match name.LastIndexOf('.') with
                | -1 -> failwith "expected type.method"
                | index -> name.Substring(0, index), name.Substring(index + 1)

            let t = getType typeName
            match t.GetMethod(name = methodName,
                                bindingAttr = (BindingFlags.Public ||| BindingFlags.Instance ||| BindingFlags.Static),
                                binder = null,
                                types = Array.ofList argTypes,
                                modifiers = null) with
            | null -> failwithf "no method on %s matching %s %A" t.FullName methodName argTypes
            | mi -> mi

        let parseOperand (opCode : OpCode) (operands : Expr<_> list) : obj option =
            match opCode.OperandType with
            | OperandType.InlineMethod ->
                match operands with
                | Atom(_, name) :: types ->
                    let mi =
                        types
                        |> List.map (function
                            | Atom(_, name) -> getType name
                            | o -> failwithf "expected a type name, not %A" o)
                        |> getMethod name

                    Some <| box mi

                | o -> failwithf "expected a method name, not %A" o

            | OperandType.InlineI ->
                match operands with
                | [Int(_, n)] -> Some <| box n
                | o -> failwithf "expected an integer, not %A" o

            | OperandType.InlineI8 ->
                match operands with
                | [Int(_, n)] -> Some <| box (byte n)
                | o -> failwithf "expected an integer, not %A" o

            | OperandType.InlineR ->
                match operands with
                | [Float(_, n)] -> Some <| box n
                | [Int(_, n)] -> Some <| box (float n)
                | o -> failwithf "expected a number, not %A" o

            | OperandType.InlineNone ->
                match operands with
                | [] -> None
                | o -> failwithf "didn't expect operand %A" o

            | OperandType.InlineString ->
                match operands with
                | [String(_, s)] -> Some <| box s
                | o -> failwithf "expected a string, not %A" o

            | OperandType.InlineType ->
                match operands with
                | [Atom(_, name)] ->
                    let t = getType name
                    Some <| box t

                | o -> failwithf "expected a type name, not %A" o

            | OperandType.InlineBrTarget
            | OperandType.InlineField
            | OperandType.InlineSig
            | OperandType.InlineSwitch
            | OperandType.InlineTok
            | OperandType.InlineVar
            | OperandType.ShortInlineBrTarget
            | OperandType.ShortInlineI
            | OperandType.ShortInlineR
            | OperandType.ShortInlineVar
            | _ -> failwith "asm operands of type %A are not supported" opCode.OperandType

        let makeAsm (opCodeName : string) (operands : Expr<_> list) (resultTypeName : string) (stack : Expr<_> list) : Asm<Expr<_>> =
            let fieldInfo = 
                typeof<OpCodes>.GetField(
                    opCodeName.Replace(".", "_"), 
                    BindingFlags.Public ||| BindingFlags.Static ||| BindingFlags.IgnoreCase)

            if fieldInfo = null then
                failwithf "invalid opcode %s" opCodeName

            let opCode : OpCode = unbox <| fieldInfo.GetValue(null)

            {
                OpCode = opCode
                Operand = parseOperand opCode operands
                ResultType = getType resultTypeName
                Stack = stack
            }

        match expr with
        | List(_, Atom(_, ".asm") :: Atom(_, opCodeName) :: Atom(_, resultTypeName) :: stack) ->
            Some <| makeAsm opCodeName [] resultTypeName stack

        | List(_, Atom(_, ".asm") :: List(_, Atom(_, opCodeName) :: operands) :: Atom(_, resultTypeName) :: stack) ->
            Some <| makeAsm opCodeName operands resultTypeName stack

        | _ ->
            None
