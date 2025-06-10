// Diff syntax highlighting functionality
function highlightDiffCode() {
    const codeBlocks = document.querySelectorAll('pre');
    
    codeBlocks.forEach(block => {
        if (block.classList.contains('diff-processed')) return;
        
        const content = block.textContent;
        const lines = content.split('\n');
        
        // Check if this looks like a diff
        const isDiff = lines.some(line => 
            line.startsWith('+') || line.startsWith('-') || line.startsWith('@@')
        );
        
        if (isDiff) {
            block.innerHTML = '';
            block.classList.add('diff-processed');
            
            lines.forEach(line => {
                const span = document.createElement('span');
                span.className = 'diff-line';
                
                if (line.startsWith('+')) {
                    span.className += ' diff-added';
                } else if (line.startsWith('-')) {
                    span.className += ' diff-removed';
                } else if (line.startsWith('@@')) {
                    span.className += ' diff-header';
                } else {
                    span.className += ' diff-context';
                }
                
                span.textContent = line + '\n';
                block.appendChild(span);
            });
        }
    });
}

// Token types for C++ highlighting
const TOKEN_TYPES = {
    COMMENT: 'comment',
    STRING: 'string',
    PREPROCESSOR: 'preprocessor',
    KEYWORD: 'keyword',
    TYPE: 'type',
    NUMBER: 'number',
    FUNCTION: 'function',
    NAMESPACE: 'namespace',
    NORMAL: 'normal'
};

// C++ syntax highlighting functionality
function highlightCppCode() {
    const codeBlocks = document.querySelectorAll('pre');
    
    codeBlocks.forEach(block => {
        if (block.classList.contains('cpp-processed') || block.classList.contains('diff-processed')) return;
        
        const content = block.textContent;
        
        // Check if this looks like C++ code
        const isCpp = content.includes('#include') || 
                      content.includes('namespace') || 
                      content.includes('class') ||
                      content.includes('UCLASS') ||
                      content.includes('UFUNCTION') ||
                      content.includes('UPROPERTY') ||
                      content.includes('::') ||
                      /\b(void|int|float|double|bool|char|auto)\s+\w+\s*\(/.test(content);
        
        if (isCpp) {
            block.classList.add('cpp-processed');
            
            // Tokenize the content to avoid overlapping replacements
            const tokens = tokenizeCpp(content);
            
            // Build HTML from tokens
            let html = '';
            tokens.forEach(token => {
                const escapedText = escapeHtml(token.text);
                if (token.type === TOKEN_TYPES.NORMAL) {
                    html += escapedText;
                } else {
                    html += `<span class="cpp-${token.type}">${escapedText}</span>`;
                }
            });
            
            block.innerHTML = html;
        }
    });
}

// Escape HTML entities
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Tokenize C++ code into typed tokens
function tokenizeCpp(code) {
    const tokens = [];
    let i = 0;
    
    while (i < code.length) {
        // Skip whitespace but preserve it
        if (/\s/.test(code[i])) {
            let start = i;
            while (i < code.length && /\s/.test(code[i])) {
                i++;
            }
            tokens.push({ type: TOKEN_TYPES.NORMAL, text: code.substring(start, i) });
            continue;
        }
        
        // Multi-line comments
        if (code.substr(i, 2) === '/*') {
            let start = i;
            i += 2;
            while (i < code.length - 1 && code.substr(i, 2) !== '*/') {
                i++;
            }
            if (code.substr(i, 2) === '*/') {
                i += 2;
            }
            tokens.push({ type: TOKEN_TYPES.COMMENT, text: code.substring(start, i) });
            continue;
        }
        
        // Single line comments
        if (code.substr(i, 2) === '//') {
            let start = i;
            while (i < code.length && code[i] !== '\n') {
                i++;
            }
            tokens.push({ type: TOKEN_TYPES.COMMENT, text: code.substring(start, i) });
            continue;
        }
        
        // String literals
        if (code[i] === '"' || code[i] === "'") {
            let quote = code[i];
            let start = i;
            i++;
            while (i < code.length && code[i] !== quote) {
                if (code[i] === '\\' && i + 1 < code.length) {
                    i += 2; // Skip escaped character
                } else {
                    i++;
                }
            }
            if (i < code.length) {
                i++; // Include closing quote
            }
            tokens.push({ type: TOKEN_TYPES.STRING, text: code.substring(start, i) });
            continue;
        }
        
        // Preprocessor directives
        if (code[i] === '#' && (i === 0 || code[i-1] === '\n' || /\s/.test(code[i-1]))) {
            let start = i;
            while (i < code.length && code[i] !== '\n') {
                i++;
            }
            tokens.push({ type: TOKEN_TYPES.PREPROCESSOR, text: code.substring(start, i) });
            continue;
        }
        
        // Numbers
        if (/\d/.test(code[i])) {
            let start = i;
            while (i < code.length && /[\d.f]/.test(code[i])) {
                i++;
            }
            tokens.push({ type: TOKEN_TYPES.NUMBER, text: code.substring(start, i) });
            continue;
        }
        
        // Identifiers and keywords
        if (/[a-zA-Z_]/.test(code[i])) {
            let start = i;
            while (i < code.length && /[a-zA-Z0-9_]/.test(code[i])) {
                i++;
            }
            let word = code.substring(start, i);
            
            // Check if it's a keyword
            const keywords = [
                'class', 'struct', 'namespace', 'public', 'private', 'protected',
                'virtual', 'override', 'static', 'const', 'inline', 'template',
                'typename', 'typedef', 'using', 'if', 'else', 'for', 'while',
                'do', 'switch', 'case', 'default', 'break', 'continue', 'return',
                'try', 'catch', 'throw', 'new', 'delete', 'this', 'nullptr',
                'true', 'false', 'auto', 'void', 'int', 'float', 'double',
                'bool', 'char', 'unsigned', 'signed', 'long', 'short', 'enum',
                'union', 'sizeof', 'alignof', 'decltype', 'noexcept', 'constexpr',
                'mutable', 'volatile', 'extern', 'register', 'thread_local'
            ];
            
            // Check if it's a type
            const types = [
                'FString', 'FText', 'FName', 'FVector', 'FVector2D', 'FVector4',
                'FRotator', 'FTransform', 'FQuat', 'FColor', 'FLinearColor',
                'TArray', 'TMap', 'TSet', 'TSharedPtr', 'TWeakPtr', 'TUniquePtr',
                'UCLASS', 'USTRUCT', 'UENUM', 'UFUNCTION', 'UPROPERTY', 'UPARAM',
                'UMETA', 'GENERATED_BODY', 'GENERATED_UCLASS_BODY',
                'DECLARE_DYNAMIC_MULTICAST_DELEGATE', 'DECLARE_MULTICAST_DELEGATE',
                'UObject', 'UActorComponent', 'USceneComponent', 'AActor', 'APawn',
                'ACharacter', 'APlayerController', 'AGameModeBase', 'UWorld',
                'UEngine', 'UGameInstance', 'FVector3f', 'FQuat4f', 'FDelegateHandle',
                'std::string', 'std::vector', 'std::map', 'std::set', 'std::shared_ptr',
                'std::unique_ptr', 'std::weak_ptr', 'std::function', 'std::array',
                'std::pair', 'std::tuple', 'size_t', 'uint8', 'uint16', 'uint32',
                'uint64', 'int8', 'int16', 'int32', 'int64'
            ];
            
            let tokenType = TOKEN_TYPES.NORMAL;
            if (keywords.includes(word)) {
                tokenType = TOKEN_TYPES.KEYWORD;
            } else if (types.includes(word)) {
                tokenType = TOKEN_TYPES.TYPE;
            } else if (i < code.length && /\s*\(/.test(code.substring(i))) {
                tokenType = TOKEN_TYPES.FUNCTION;
            }
            
            tokens.push({ type: tokenType, text: word });
            continue;
        }
        
        // Namespace resolution operator
        if (code.substr(i, 2) === '::') {
            tokens.push({ type: TOKEN_TYPES.NAMESPACE, text: '::' });
            i += 2;
            continue;
        }
        
        // Other characters
        tokens.push({ type: TOKEN_TYPES.NORMAL, text: code[i] });
        i++;
    }
    
    return tokens;
}

// Initialize syntax highlighting when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    highlightDiffCode();
    highlightCppCode();
    
    // Also run when new content is dynamically added
    const observer = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.type === 'childList' && mutation.addedNodes.length > 0) {
                highlightDiffCode();
                highlightCppCode();
            }
        });
    });
    
    observer.observe(document.body, {
        childList: true,
        subtree: true
    });
}); 