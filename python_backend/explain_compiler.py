#!/usr/bin/env python3
"""
Demo: Understanding Python's Compilation Process
This script demonstrates how Python compiles and executes code.
"""

import py_compile
import dis
import os

def demonstrate_compilation():
    """Show how Python compilation works"""
    print("🐍 PYTHON COMPILATION DEMO")
    print("=" * 50)
    
    # 1. Create a simple Python file
    sample_code = '''
def greet(name):
    """A simple greeting function"""
    message = f"Hello, {name}!"
    return message

if __name__ == "__main__":
    result = greet("Python Learner")
    print(result)
'''
    
    # Write sample code to a file
    with open("sample_code.py", "w") as f:
        f.write(sample_code)
    
    print("✅ 1. Created sample_code.py")
    
    # 2. Manually compile to bytecode
    print("\n🔧 2. COMPILATION STEP:")
    py_compile.compile("sample_code.py", "sample_code.pyc")
    print("   ✅ Compiled sample_code.py → sample_code.pyc")
    
    # Check file sizes
    py_size = os.path.getsize("sample_code.py")
    pyc_size = os.path.getsize("sample_code.pyc")
    print(f"   📁 Source file (.py): {py_size} bytes")
    print(f"   📁 Compiled file (.pyc): {pyc_size} bytes")
    
    # 3. Show what bytecode looks like
    print("\n🔍 3. BYTECODE ANALYSIS:")
    print("   This is what the Python Virtual Machine actually executes:")
    print("   " + "-" * 45)
    
    # Compile and show bytecode for a simple function
    def simple_function():
        x = 5
        y = 10
        return x + y
    
    dis.dis(simple_function)
    
    # 4. Execution step
    print("\n🚀 4. EXECUTION STEP:")
    print("   Now running the compiled code...")
    
    # Import and run the compiled module
    import sample_code
    
    # Cleanup
    print("\n🧹 Cleaning up demo files...")
    try:
        os.remove("sample_code.py")
        os.remove("sample_code.pyc")
        print("   ✅ Demo files removed")
    except:
        print("   ⚠️  Some files may still exist")

def compare_languages():
    """Compare Python with other language compilation models"""
    print("\n" + "=" * 60)
    print("🔄 COMPILATION MODELS COMPARISON")
    print("=" * 60)
    
    languages = {
        "C/C++": {
            "model": "Fully Compiled",
            "process": "Source → Machine Code",
            "when": "Before running",
            "output": "Executable (.exe)",
            "speed": "Very Fast execution",
            "portability": "Platform-specific"
        },
        "Java": {
            "model": "Compile to Bytecode",
            "process": "Source → Bytecode → Virtual Machine",
            "when": "Before running",
            "output": "Bytecode (.class)",
            "speed": "Fast execution",
            "portability": "Cross-platform"
        },
        "Python": {
            "model": "Compile + Interpret",
            "process": "Source → Bytecode → Virtual Machine",
            "when": "At runtime (automatic)",
            "output": "Bytecode (.pyc)",
            "speed": "Slower execution",
            "portability": "Cross-platform"
        },
        "JavaScript": {
            "model": "Interpreted (+ JIT)",
            "process": "Source → Parse → Execute",
            "when": "At runtime",
            "output": "None (+ optimized code)",
            "speed": "Medium execution",
            "portability": "Cross-platform"
        }
    }
    
    for lang, details in languages.items():
        print(f"\n📝 {lang}:")
        for key, value in details.items():
            print(f"   {key.capitalize()}: {value}")

def python_compilation_facts():
    """Key facts about Python compilation"""
    print("\n" + "=" * 60)
    print("🎯 KEY PYTHON COMPILATION FACTS")
    print("=" * 60)
    
    facts = [
        "✅ Python IS compiled - just not to machine code",
        "✅ .pyc files are compiled bytecode",
        "✅ Compilation happens automatically when you run .py files",
        "✅ Python caches .pyc files to speed up future runs",
        "✅ The Python Virtual Machine executes bytecode",
        "✅ You rarely need to think about compilation in Python",
        "⚠️  Python is slower than C++ because of the interpretation layer",
        "⚠️  But it's much faster to develop and debug!",
        "💡 Modern Python uses optimizations (like PyPy JIT compiler)",
        "💡 You can use tools like PyInstaller to create .exe files"
    ]
    
    for i, fact in enumerate(facts, 1):
        print(f"{i:2d}. {fact}")

def when_compilation_happens():
    """Explain when Python compilation occurs"""
    print("\n" + "=" * 60)
    print("⏰ WHEN DOES PYTHON COMPILATION HAPPEN?")
    print("=" * 60)
    
    scenarios = [
        {
            "scenario": "First time running a .py file",
            "what_happens": [
                "Python reads your .py file",
                "Compiles it to bytecode",
                "Saves .pyc file in __pycache__ folder",
                "Executes the bytecode"
            ]
        },
        {
            "scenario": "Running the same .py file again",
            "what_happens": [
                "Python checks if .pyc file exists",
                "Checks if .py file was modified",
                "If not modified: uses existing .pyc (faster!)",
                "If modified: recompiles to new .pyc"
            ]
        },
        {
            "scenario": "Importing a module",
            "what_happens": [
                "Same process as running a file",
                "Each imported .py file gets compiled",
                "Compiled modules cached for future imports"
            ]
        }
    ]
    
    for scenario in scenarios:
        print(f"\n📋 {scenario['scenario']}:")
        for step in scenario['what_happens']:
            print(f"   • {step}")

if __name__ == "__main__":
    print("🐍 Welcome to Python Compilation Explained!")
    print("This demo will show you how Python compilation really works.\n")
    
    try:
        demonstrate_compilation()
        compare_languages()
        python_compilation_facts()
        when_compilation_happens()
        
        print("\n" + "=" * 60)
        print("🎉 SUMMARY")
        print("=" * 60)
        print("Python uses a hybrid approach:")
        print("📝 1. Your .py files are SOURCE CODE (human-readable)")
        print("🔧 2. Python COMPILES them to .pyc BYTECODE (machine-readable)")
        print("🚀 3. Python Virtual Machine INTERPRETS the bytecode")
        print("💾 4. .pyc files are CACHED for faster subsequent runs")
        print("\nThis gives you the best of both worlds:")
        print("✅ Easy development (like interpreted languages)")
        print("✅ Better performance (thanks to compilation + caching)")
        
    except Exception as e:
        print(f"❌ Error during demo: {e}")
        print("But the explanation above still applies!")
