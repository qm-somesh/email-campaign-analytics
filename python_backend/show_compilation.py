#!/usr/bin/env python3
"""
Show Python Compilation in Your Project 🔍
This script examines your FastAPI project to show compiled files.
"""

import os
import time
from pathlib import Path

def find_python_files(directory):
    """Find all .py files in the project"""
    python_files = []
    for root, dirs, files in os.walk(directory):
        # Skip __pycache__ directories
        dirs[:] = [d for d in dirs if d != '__pycache__']
        for file in files:
            if file.endswith('.py'):
                python_files.append(os.path.join(root, file))
    return sorted(python_files)

def find_compiled_files(directory):
    """Find all .pyc files in the project"""
    compiled_files = []
    for root, dirs, files in os.walk(directory):
        for file in files:
            if file.endswith('.pyc'):
                compiled_files.append(os.path.join(root, file))
    return sorted(compiled_files)

def get_file_info(filepath):
    """Get file information"""
    if os.path.exists(filepath):
        stat = os.stat(filepath)
        return {
            'size': stat.st_size,
            'modified': time.ctime(stat.st_mtime)
        }
    return None

def main():
    """Main function to analyze the project"""
    print("=" * 70)
    print(" 🐍 Python Compilation Analysis - Your FastAPI Project")
    print("=" * 70)
    
    project_root = "."
    
    # Find all Python files
    py_files = find_python_files(project_root)
    pyc_files = find_compiled_files(project_root)
    
    print(f"\n📊 Project Analysis:")
    print(f"   📝 Python source files (.py): {len(py_files)}")
    print(f"   🤖 Compiled bytecode files (.pyc): {len(pyc_files)}")
    
    print(f"\n📁 Your Python Source Files:")
    print("-" * 50)
    
    for py_file in py_files:
        info = get_file_info(py_file)
        if info:
            rel_path = os.path.relpath(py_file)
            print(f"📄 {rel_path}")
            print(f"   Size: {info['size']} bytes")
            print(f"   Modified: {info['modified']}")
            
            # Check if there's a corresponding .pyc file
            possible_pyc = py_file.replace('.py', '.cpython-39.pyc')
            cache_dir = os.path.dirname(py_file)
            cache_pyc = os.path.join(cache_dir, '__pycache__', 
                                   os.path.basename(possible_pyc))
            
            if os.path.exists(cache_pyc):
                pyc_info = get_file_info(cache_pyc)
                print(f"   🤖 Bytecode: {os.path.relpath(cache_pyc)}")
                print(f"   🤖 Size: {pyc_info['size']} bytes")
                print(f"   🤖 Modified: {pyc_info['modified']}")
            else:
                print(f"   ⚪ No bytecode found (will be created on first import)")
            print()
    
    if pyc_files:
        print(f"\n🤖 Compiled Bytecode Files Found:")
        print("-" * 50)
        
        for pyc_file in pyc_files:
            info = get_file_info(pyc_file)
            rel_path = os.path.relpath(pyc_file)
            print(f"🤖 {rel_path}")
            print(f"   Size: {info['size']} bytes")
            print(f"   Modified: {info['modified']}")
            
            # Try to find the source file
            source_name = os.path.basename(pyc_file).split('.')[0] + '.py'
            source_dir = os.path.dirname(os.path.dirname(pyc_file))  # Go up from __pycache__
            source_path = os.path.join(source_dir, source_name)
            
            if os.path.exists(source_path):
                print(f"   📄 Source: {os.path.relpath(source_path)}")
            print()
    
    print("\n💡 What This Means:")
    print("-" * 50)
    print("✅ .py files = Your source code (what you edit)")
    print("🤖 .pyc files = Compiled bytecode (created automatically)")
    print("📁 __pycache__ folders = Where Python stores compiled bytecode")
    print("⚡ Having .pyc files means faster startup times")
    print("🗑️ You can safely delete __pycache__ folders anytime")
    
    print("\n🔄 What Happens When You Run Your Server:")
    print("-" * 50)
    print("1. 🔍 Python checks if .pyc files exist and are up-to-date")
    print("2. 🔧 If not, compiles .py files to .pyc files")
    print("3. 📁 Stores .pyc files in __pycache__ folders")
    print("4. ⚡ Loads and executes the bytecode")
    print("5. 🚀 Your FastAPI server starts!")
    
    # Cache cleanup option
    print("\n🧹 Cache Management:")
    print("-" * 50)
    
    cache_dirs = []
    for root, dirs, files in os.walk(project_root):
        if '__pycache__' in dirs:
            cache_dirs.extend([os.path.join(root, d) for d in dirs if d == '__pycache__'])
    
    if cache_dirs:
        print(f"Found {len(cache_dirs)} __pycache__ directories:")
        for cache_dir in cache_dirs:
            print(f"   📁 {os.path.relpath(cache_dir)}")
        
        print("\n❓ Want to clean up cache files? Run this command:")
        print("   PowerShell: Get-ChildItem -Path . -Recurse -Name '__pycache__' | Remove-Item -Recurse -Force")
        print("   Or use: python -m py_compile *.py (to regenerate)")
    else:
        print("No __pycache__ directories found.")
        print("💡 They'll be created when you run your FastAPI server!")

if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        print(f"❌ Error: {e}")
        print("💡 Make sure you're running this from your project root directory!")
