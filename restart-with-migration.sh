#!/bin/zsh

echo "🔄 Stopping existing application..."
lsof -ti:5200 | xargs kill -9 2>/dev/null
sleep 2

echo "🚀 Starting application with database migrations..."
cd /Users/abhikporel/dev/Hotelapp/HotelApp.Web
dotnet run --launch-profile http
