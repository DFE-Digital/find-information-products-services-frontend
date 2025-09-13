#!/bin/bash

# Bash script to run tests with coverage and reporting

FILTER=""
COVERAGE=false
DETAILED=false
OUTPUT_DIR="TestResults"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --filter)
            FILTER="$2"
            shift 2
            ;;
        --coverage)
            COVERAGE=true
            shift
            ;;
        --detailed)
            DETAILED=true
            shift
            ;;
        --output)
            OUTPUT_DIR="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo "Options:"
            echo "  --filter FILTER    Filter tests by name"
            echo "  --coverage         Generate coverage report"
            echo "  --detailed         Show detailed test output"
            echo "  --output DIR       Output directory for results"
            echo "  -h, --help         Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option $1"
            exit 1
            ;;
    esac
done

echo "Running FIPS Frontend Tests..."

# Create output directory if it doesn't exist
mkdir -p "$OUTPUT_DIR"

# Build the solution first
echo "Building solution..."
dotnet build --configuration Release

if [ $? -ne 0 ]; then
    echo "Build failed!"
    exit 1
fi

# Prepare test command
TEST_COMMAND="dotnet test --configuration Release --no-build"

if [ "$COVERAGE" = true ]; then
    TEST_COMMAND="$TEST_COMMAND --collect:\"XPlat Code Coverage\" --results-directory \"$OUTPUT_DIR\""
fi

if [ -n "$FILTER" ]; then
    TEST_COMMAND="$TEST_COMMAND --filter \"$FILTER\""
fi

if [ "$DETAILED" = true ]; then
    TEST_COMMAND="$TEST_COMMAND --logger \"console;verbosity=detailed\""
else
    TEST_COMMAND="$TEST_COMMAND --logger \"console;verbosity=normal\""
fi

# Run tests
echo "Running tests..."
eval $TEST_COMMAND

if [ $? -eq 0 ]; then
    echo "All tests passed!"
    
    if [ "$COVERAGE" = true ]; then
        echo "Coverage report generated in: $OUTPUT_DIR"
        
        # Try to find and display coverage summary
        COVERAGE_FILE=$(find "$OUTPUT_DIR" -name "coverage.cobertura.xml" | head -n 1)
        if [ -n "$COVERAGE_FILE" ]; then
            echo "Coverage file: $COVERAGE_FILE"
        fi
    fi
else
    echo "Some tests failed!"
    exit 1
fi

echo "Test run completed."
