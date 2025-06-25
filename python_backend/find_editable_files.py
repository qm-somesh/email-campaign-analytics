#!/usr/bin/env python3
"""
Helper script to find editable source files in the Python backend
This helps beginners identify which files they can actually edit
"""

import os
import fnmatch

def find_editable_files(root_dir=".", show_binary=False):
    """Find files that are safe and useful to edit"""
    
    # File extensions that are editable/readable
    editable_extensions = {
        '.py': 'Python source code',
        '.md': 'Markdown documentation',
        '.txt': 'Text files (requirements, etc.)',
        '.env': 'Environment configuration',
        '.json': 'JSON configuration',
        '.yaml': 'YAML configuration',
        '.yml': 'YAML configuration',
        '.bat': 'Windows batch scripts',
        '.sh': 'Shell scripts',
        '.sql': 'SQL scripts',
        '.html': 'HTML files',
        '.css': 'CSS stylesheets',
        '.js': 'JavaScript files'
    }
    
    # File patterns to ignore (binary/cache files)
    ignore_patterns = [
        '*.pyc',
        '__pycache__',
        '*.pyo',
        '*.pyd',
        '.git',
        'venv',
        'node_modules',
        '*.exe',
        '*.dll',
        '*.so',
        '*.dylib',
        '*.class',
        '*.jar'
    ]
    
    editable_files = []
    binary_files = []
    
    for root, dirs, files in os.walk(root_dir):
        # Remove ignored directories from the search
        dirs[:] = [d for d in dirs if not any(fnmatch.fnmatch(d, pattern) for pattern in ignore_patterns)]
        
        for file in files:
            file_path = os.path.join(root, file)
            relative_path = os.path.relpath(file_path, root_dir)
            
            # Skip if matches ignore patterns
            if any(fnmatch.fnmatch(file, pattern) for pattern in ignore_patterns):
                binary_files.append(relative_path)
                continue
            
            # Check file extension
            _, ext = os.path.splitext(file)
            ext = ext.lower()
            
            if ext in editable_extensions:
                editable_files.append({
                    'path': relative_path,
                    'type': editable_extensions[ext],
                    'size': os.path.getsize(file_path)
                })
            else:
                binary_files.append(relative_path)
    
    return editable_files, binary_files

def print_file_guide():
    """Print a guide showing which files to edit"""
    print("=" * 80)
    print("🐍 PYTHON BACKEND - EDITABLE FILES GUIDE")
    print("=" * 80)
    
    current_dir = os.getcwd()
    if 'python_backend' not in current_dir:
        print("⚠️  Note: Run this from the python_backend directory for best results")
        print()
    
    editable, binary = find_editable_files()
    
    print("✅ FILES YOU CAN EDIT (Source Code & Configuration):")
    print("-" * 60)
    
    # Group by type
    by_type = {}
    for file_info in editable:
        file_type = file_info['type']
        if file_type not in by_type:
            by_type[file_type] = []
        by_type[file_type].append(file_info)
    
    for file_type, files in by_type.items():
        print(f"\n📁 {file_type}:")
        for file_info in sorted(files, key=lambda x: x['path']):
            size_kb = file_info['size'] / 1024
            print(f"   • {file_info['path']:<50} ({size_kb:.1f} KB)")
    
    print(f"\n🚫 FILES TO IGNORE (Binary/Cache files): {len(binary)} files")
    print("-" * 60)
    print("These files are automatically generated and should not be edited:")
    
    # Show just a few examples
    binary_examples = [f for f in binary if any(pattern in f for pattern in ['__pycache__', '.pyc', 'venv'])]
    for example in binary_examples[:10]:  # Show first 10
        print(f"   • {example}")
    
    if len(binary_examples) > 10:
        print(f"   ... and {len(binary_examples) - 10} more cache/binary files")
    
    print("\n🎯 MOST IMPORTANT FILES FOR BEGINNERS:")
    print("-" * 60)
    important_files = [
        ("start.py", "🚀 Start the web server"),
        ("app/main.py", "🌐 Main FastAPI application setup"),
        ("app/controllers/natural_language_sql_controller.py", "🎮 API endpoints (routes)"),
        ("app/services/natural_sql_rag/orchestrator.py", "🎭 Main business logic"),
        ("app/services/natural_sql_rag/rag_service.py", "📚 Knowledge base service"),
        ("app/services/natural_sql_rag/gemini_service.py", "🤖 AI service"),
        (".env", "⚙️ Configuration (API keys, database)"),
        ("requirements.txt", "📦 Python packages needed"),
        ("PYTHON_BEGINNER_GUIDE.md", "📖 Learning guide"),
    ]
    
    for file_path, description in important_files:
        if os.path.exists(file_path):
            print(f"   ✅ {file_path:<45} {description}")
        else:
            print(f"   ❌ {file_path:<45} {description} (not found)")
    
    print("\n💡 QUICK TIPS:")
    print("-" * 60)
    print("• Open .py files in your text editor (VS Code, PyCharm, etc.)")
    print("• Never edit .pyc files - they're compiled binary code")
    print("• The venv/ folder contains your virtual environment - don't edit")
    print("• __pycache__/ folders are automatic - safe to delete")
    print("• Start with reading the main.py and controller files")
    print("• Use 'python start.py' to run the server")
    
    print(f"\n📊 SUMMARY:")
    print(f"   Editable files: {len(editable)}")
    print(f"   Binary/cache files: {len(binary)}")
    print(f"   Total files: {len(editable) + len(binary)}")

def clean_cache_files():
    """Remove all .pyc files and __pycache__ directories"""
    print("🧹 Cleaning cache files...")
    
    removed_count = 0
    for root, dirs, files in os.walk("."):
        # Remove __pycache__ directories
        if '__pycache__' in dirs:
            import shutil
            cache_dir = os.path.join(root, '__pycache__')
            shutil.rmtree(cache_dir)
            print(f"   Removed: {cache_dir}")
            removed_count += 1
        
        # Remove .pyc files
        for file in files:
            if file.endswith('.pyc'):
                file_path = os.path.join(root, file)
                os.remove(file_path)
                print(f"   Removed: {file_path}")
                removed_count += 1
    
    if removed_count == 0:
        print("   No cache files found to clean")
    else:
        print(f"   Cleaned {removed_count} cache files/directories")
    
    print("   ✅ Cache cleanup complete!")

if __name__ == "__main__":
    print("🔍 What would you like to do?")
    print("1. Show editable files guide")
    print("2. Clean cache files (.pyc, __pycache__)")
    print("3. Both")
    
    choice = input("\nEnter choice (1, 2, or 3): ").strip()
    
    if choice in ["1", "3"]:
        print()
        print_file_guide()
    
    if choice in ["2", "3"]:
        print()
        clean_cache_files()
    
    print("\n🎉 Done!")
    print("\nRemember: Only edit .py, .md, .txt, .env, and .json files!")
    print("Never edit .pyc files or anything in __pycache__ folders!")
