﻿#light
namespace Tim.Lisp.Core
open System.Reflection.Emit

module Evaluator =
    let extractAtom = function
        | Atom a -> a
        | v -> failwith ("expected atom, got " + any_to_string v)

    let rec insertPrimitives = function
        | List (Atom "+" :: args) -> ListPrimitive (Add, args |> List.map insertPrimitives)
        | List (Atom "-" :: args) -> ListPrimitive (Subtract, args |> List.map insertPrimitives)
        | List (Atom "*" :: args) -> ListPrimitive (Multiply, args |> List.map insertPrimitives)
        | List (Atom "/" :: args) -> ListPrimitive (Divide, args |> List.map insertPrimitives)
        | List (Atom "eval" :: args) -> 
            match args with
            | [ v ] -> UnaryPrimitive (Eval, insertPrimitives v)
            | _ -> failwith "expected one item for eval"

        | List (Atom "quote" :: args) -> 
            match args with
            | [ v ] -> UnaryPrimitive (Quote, insertPrimitives v)
            | _ -> failwith "expected one item for quote"

        | List (Atom "lambda" :: args) ->
            match args with
            | [ List names; body ] -> LambdaPrimitive (names |> List.map extractAtom, insertPrimitives body)
            | _ -> failwith "expected lambda names body"

        | List (Atom "vr" :: args) ->
            match args with
            | [ Atom name; v ] -> VariablePrimitive (name, insertPrimitives v)
            | _ -> failwith "expected vr name value"

        | List l -> l |> List.map insertPrimitives |> List
        | v -> v

    let boxUnbox<'a> fn = fun a b -> fn (unbox<'a> a) (unbox<'a> b) |> box

    let rec eval (env : Map<string, LispVal>) = 
        let evalBinary op args = 
            let fn = 
                match op with
                | Add -> (+)
                | Subtract -> (-)
                | Multiply -> (*)
                | Divide -> (/)

            (env, args |> List.map (eval env >> snd) |> List.reduce_left (boxUnbox fn))

        function
        | Atom a -> (env, env.[a] |> eval env |> snd)
        | Bool b as v -> (env, box b)
        | CompiledLambda _ -> failwith "didn't expect CompiledLambda"
        | CompiledVariable _ -> failwith "didn't expect CompiledVariable"
        | LambdaPrimitive (names, code) -> failwith "cannot evaluate lambda"
        | List l -> failwith ("cannot evaluate list " + any_to_string l)
        | ListPrimitive (op, args) -> evalBinary op args
        | Number n as v -> (env, box n)
        | String s as v -> (env, box s)
        | UnaryPrimitive (Eval, arg) -> (env, arg |> insertPrimitives |> eval env |> snd)
        | UnaryPrimitive (Quote, arg) -> (env, box arg)
        | VariablePrimitive (name, value) -> (Map.add name value env, box value)