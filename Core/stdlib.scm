﻿(.ref "mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")
(.ref "System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")
(define (+ a b) (.asm add System.Int32 a b))
(define (- a b) (.asm sub System.Int32 a b))
(define (* a b) (.asm mul System.Int32 a b))
