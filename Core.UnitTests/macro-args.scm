﻿(defmacro (greet who)
          (.asm (call Console.Write String) Void "hello ")
		  (.asm (call Console.WriteLine String) Void who))
(greet "world")
(greet "Tim")
