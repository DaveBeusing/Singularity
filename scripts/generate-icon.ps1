# Copyright (c) 2026 David Beusing <david.beusing@gmail.com>
# Licensed under the MIT License.
# See LICENSE file in the project root for full license information.

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$repoRoot = Split-Path -Parent $PSScriptRoot
$assetRoot = Join-Path $repoRoot "src\Singularity\Assets"
$sourceRoot = Join-Path $assetRoot "Source"
$masterPath = Join-Path $sourceRoot "Singularity-master.png"
$iconPath = Join-Path $assetRoot "Singularity.ico"
$iconSizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)

function New-RoundedRectanglePath {
	param(
		[float] $X,
		[float] $Y,
		[float] $Width,
		[float] $Height,
		[float] $Radius
	)

	$diameter = $Radius * 2
	$path = [System.Drawing.Drawing2D.GraphicsPath]::new()
	$path.AddArc($X, $Y, $diameter, $diameter, 180, 90)
	$path.AddArc($X + $Width - $diameter, $Y, $diameter, $diameter, 270, 90)
	$path.AddArc($X + $Width - $diameter, $Y + $Height - $diameter, $diameter, $diameter, 0, 90)
	$path.AddArc($X, $Y + $Height - $diameter, $diameter, $diameter, 90, 90)
	$path.CloseFigure()
	return $path
}

function New-UpperArcPath {
	param([float] $Size, [bool] $Simplified)

	$path = [System.Drawing.Drawing2D.GraphicsPath]::new()
	$path.StartFigure()
	$path.AddBezier(0.205 * $Size, 0.455 * $Size, 0.170 * $Size, 0.285 * $Size, 0.335 * $Size, 0.190 * $Size, 0.520 * $Size, 0.205 * $Size)
	$path.AddBezier(0.520 * $Size, 0.205 * $Size, 0.675 * $Size, 0.215 * $Size, 0.785 * $Size, 0.285 * $Size, 0.830 * $Size, 0.390 * $Size)
	$path.AddBezier(0.830 * $Size, 0.390 * $Size, 0.755 * $Size, 0.365 * $Size, 0.690 * $Size, 0.360 * $Size, 0.625 * $Size, 0.370 * $Size)
	$path.AddBezier(0.625 * $Size, 0.370 * $Size, 0.575 * $Size, 0.305 * $Size, 0.485 * $Size, 0.285 * $Size, 0.405 * $Size, 0.315 * $Size)
	$innerEnd = if ($Simplified) { 0.475 } else { 0.465 }
	$path.AddBezier(0.405 * $Size, 0.315 * $Size, 0.325 * $Size, 0.345 * $Size, 0.295 * $Size, 0.405 * $Size, $innerEnd * $Size, $innerEnd * $Size)
	$path.AddBezier($innerEnd * $Size, $innerEnd * $Size, 0.360 * $Size, 0.490 * $Size, 0.270 * $Size, 0.480 * $Size, 0.205 * $Size, 0.455 * $Size)
	$path.CloseFigure()
	return $path
}

function New-LowerArcPath {
	param([float] $Size, [bool] $Simplified)

	$upper = New-UpperArcPath -Size $Size -Simplified $Simplified
	$matrix = [System.Drawing.Drawing2D.Matrix]::new()
	$matrix.RotateAt(180, [System.Drawing.PointF]::new(0.5 * $Size, 0.5 * $Size))
	$upper.Transform($matrix)
	$matrix.Dispose()
	return $upper
}

function New-StarPath {
	param([float] $Size, [bool] $Simplified)

	$center = 0.5 * $Size
	$verticalFactor = if ($Simplified) { 0.115 } else { 0.100 }
	$horizontalFactor = if ($Simplified) { 0.095 } else { 0.082 }
	$waistFactor = if ($Simplified) { 0.030 } else { 0.024 }
	$vertical = $verticalFactor * $Size
	$horizontal = $horizontalFactor * $Size
	$waist = $waistFactor * $Size
	$points = [System.Drawing.PointF[]] @(
		[System.Drawing.PointF]::new($center, $center - $vertical),
		[System.Drawing.PointF]::new($center + $waist, $center - $waist),
		[System.Drawing.PointF]::new($center + $horizontal, $center),
		[System.Drawing.PointF]::new($center + $waist, $center + $waist),
		[System.Drawing.PointF]::new($center, $center + $vertical),
		[System.Drawing.PointF]::new($center - $waist, $center + $waist),
		[System.Drawing.PointF]::new($center - $horizontal, $center),
		[System.Drawing.PointF]::new($center - $waist, $center - $waist)
	)

	$path = [System.Drawing.Drawing2D.GraphicsPath]::new()
	$path.AddPolygon($points)
	return $path
}

function Write-SingularityPng {
	param(
		[int] $Size,
		[string] $Path
	)

	$simplified = $Size -le 24
	$bitmap = [System.Drawing.Bitmap]::new($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
	$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

	try {
		$graphics.Clear([System.Drawing.Color]::Transparent)
		$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
		$graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
		$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
		$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

		$tileInset = [Math]::Max(1.0, 0.055 * $Size)
		$tileSize = $Size - (2 * $tileInset)
		$tileRadius = if ($simplified) { 0.165 * $Size } else { 0.175 * $Size }
		$tilePath = New-RoundedRectanglePath -X $tileInset -Y $tileInset -Width $tileSize -Height $tileSize -Radius $tileRadius
		$tileBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 11, 16, 32))

		try {
			$graphics.FillPath($tileBrush, $tilePath)
		}
		finally {
			$tileBrush.Dispose()
			$tilePath.Dispose()
		}

		$upperPath = New-UpperArcPath -Size $Size -Simplified $simplified
		$lowerPath = New-LowerArcPath -Size $Size -Simplified $simplified

		try {
			if ($simplified) {
				$arcBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 105, 98, 255))
				try {
					$graphics.FillPath($arcBrush, $upperPath)
					$graphics.FillPath($arcBrush, $lowerPath)
				}
				finally {
					$arcBrush.Dispose()
				}
			}
			else {
				$gradientRect = [System.Drawing.RectangleF]::new(0, 0.18 * $Size, $Size, 0.64 * $Size)
				$upperBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
					$gradientRect,
					[System.Drawing.Color]::FromArgb(255, 132, 124, 255),
					[System.Drawing.Color]::FromArgb(255, 52, 49, 148),
					90)
				$lowerBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
					$gradientRect,
					[System.Drawing.Color]::FromArgb(255, 52, 49, 148),
					[System.Drawing.Color]::FromArgb(255, 109, 98, 255),
					90)

				try {
					$graphics.FillPath($upperBrush, $upperPath)
					$graphics.FillPath($lowerBrush, $lowerPath)
				}
				finally {
					$upperBrush.Dispose()
					$lowerBrush.Dispose()
				}
			}
		}
		finally {
			$upperPath.Dispose()
			$lowerPath.Dispose()
		}

		$starPath = New-StarPath -Size $Size -Simplified $simplified
		$starBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 248, 250, 255))
		try {
			$graphics.FillPath($starBrush, $starPath)
		}
		finally {
			$starBrush.Dispose()
			$starPath.Dispose()
		}

		$bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
	}
	finally {
		$graphics.Dispose()
		$bitmap.Dispose()
	}
}

function Write-MultiResolutionIcon {
	param(
		[int[]] $Sizes,
		[string] $SourceDirectory,
		[string] $Path
	)

	$images = [System.Collections.Generic.List[byte[]]]::new()
	foreach ($size in $Sizes) {
		$images.Add([System.IO.File]::ReadAllBytes((Join-Path $SourceDirectory "Singularity-$size.png")))
	}

	$stream = [System.IO.File]::Create($Path)
	$writer = [System.IO.BinaryWriter]::new($stream)

	try {
		$writer.Write([uint16] 0)
		$writer.Write([uint16] 1)
		$writer.Write([uint16] $Sizes.Count)

		$offset = 6 + (16 * $Sizes.Count)
		for ($index = 0; $index -lt $Sizes.Count; $index++) {
			$size = $Sizes[$index]
			$image = $images[$index]
			$dimension = if ($size -eq 256) { 0 } else { $size }
			$writer.Write([byte] $dimension)
			$writer.Write([byte] $dimension)
			$writer.Write([byte] 0)
			$writer.Write([byte] 0)
			$writer.Write([uint16] 1)
			$writer.Write([uint16] 32)
			$writer.Write([uint32] $image.Length)
			$writer.Write([uint32] $offset)
			$offset += $image.Length
		}

		foreach ($image in $images) {
			$writer.Write($image)
		}
	}
	finally {
		$writer.Dispose()
		$stream.Dispose()
	}
}

try {
	New-Item -ItemType Directory -Force -Path $sourceRoot | Out-Null
	Write-SingularityPng -Size 1024 -Path $masterPath

	foreach ($size in $iconSizes) {
		$variantPath = Join-Path $sourceRoot "Singularity-$size.png"
		Write-SingularityPng -Size $size -Path $variantPath
	}

	Write-MultiResolutionIcon -Sizes $iconSizes -SourceDirectory $sourceRoot -Path $iconPath
	Write-Output "Generated master: $masterPath"
	Write-Output "Generated ICO:    $iconPath"
	Write-Output "ICO sizes:        $($iconSizes -join ', ')"
}
catch {
	Write-Error "Icon generation failed: $($_.Exception.Message)"
	exit 1
}
