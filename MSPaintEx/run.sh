#!/bin/bash

# Print startup message with color
echo -e "\033[0;36mStarting MSPaintEx with debug output...\033[0m"

# Run the application and capture the exit code
dotnet run 2>&1
EXIT_CODE=$?

# If there was an error, wait for user input before closing
if [ $EXIT_CODE -ne 0 ]
then
    echo -e "\033[0;31m\nPress Enter to exit...\033[0m"
    read
fi

exit $EXIT_CODE 