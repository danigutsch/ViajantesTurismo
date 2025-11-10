// ESLint configuration for JSON files
// CI/CD: Add 'npm run lint:json' to pipeline when implemented

import jsoncPlugin from 'eslint-plugin-jsonc';
import jsoncParser from 'jsonc-eslint-parser';

export default [
    {
        files: ['**/*.json'],
        ignores: [
            '**/node_modules/**',
            '**/TestResults/**',
            '**/bin/**',
            '**/obj/**',
            '**/.vs/**',
            '**/.vscode/**',
            '**/package-lock.json'
        ],
        languageOptions: {
            parser: jsoncParser
        },
        plugins: {
            jsonc: jsoncPlugin
        },
        rules: {
            'jsonc/indent': ['error', 2],
            'jsonc/key-spacing': ['error', {beforeColon: false, afterColon: true}],
            'jsonc/no-comments': 'off', // Allow comments in JSON files (e.g., tsconfig.json)
            'jsonc/comma-dangle': ['error', 'never'],
            'jsonc/quote-props': ['error', 'always'],
            'jsonc/quotes': ['error', 'double'],
            'jsonc/array-bracket-spacing': ['error', 'never'],
            'jsonc/object-curly-spacing': ['error', 'always'],
            'jsonc/sort-keys': 'off' // Don't enforce key sorting
        }
    }
];
