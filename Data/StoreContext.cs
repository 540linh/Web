using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using ShoesLover.Models;
using System;
using System.Data;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections;
using ShoesLover.Areas.Admin.Models;

namespace ShoesLover.Data
{
    public class StoreContext
    {
        private readonly string _connectionString;
        private readonly string _rootPath;
        public StoreContext(string connString, string rootPath)
        {
            _connectionString = connString;
            _rootPath = rootPath;
        }
        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }
        public List<Product> GetProducts()
        {
            List<Product> listProduct = new List<Product>();
            try
            {
                using (var con = GetConnection())
                {
                    con.Open();
                    string str = "select * from product";
                    MySqlCommand cmd = new MySqlCommand(str, con);
                    using (var reader = cmd.ExecuteReader())
                    {

                        while (reader.Read())
                        {
                            listProduct.Add(new Product
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                ProductName = Convert.ToString(reader["productname"]),
                                SubCategoryId = Convert.ToInt32(reader["subcategory_id"]),
                                BrandId = Convert.ToInt32(reader["brand_id"]),
                                Gender = Convert.ToInt32(reader["gender"]),
                                DefaultImage = Convert.ToString(reader["default_image"]),

                                Description = Convert.ToString(reader["description"]),
                                SalePrice = Convert.ToDouble(reader["sale_price"]),
                                RegularPrice = Convert.ToDouble(reader["regular_price"]),
                                Active = Convert.ToBoolean(reader["active"])
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return listProduct;
        } //show sản phẩm liên quan 
        public List<Product> GetProductRelated(int id, int product_id)
        {
            List<Product> listProduct = new List<Product>();
            try
            {
                using (var con = GetConnection())
                {
                    con.Open();
                    string str = "select * from product " +
                        "where id!=@product_id and brand_id=@id limit 4";
                    MySqlCommand cmd = new MySqlCommand(str, con);
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.Parameters.AddWithValue("product_id", product_id);
                    using (var reader = cmd.ExecuteReader())
                    {

                        while (reader.Read())
                        {
                            listProduct.Add(new Product
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                BrandId = Convert.ToInt32(reader["brand_id"]),

                                ProductName = Convert.ToString(reader["productname"]),
                                DefaultImage = Convert.ToString(reader["default_image"]),


                                SalePrice = Convert.ToDouble(reader["sale_price"]),
                                RegularPrice = Convert.ToDouble(reader["regular_price"])

                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return listProduct;
        }

        public Product GetProductById(int id)
        {
            Product product = new Product();
            try
            {
                using (var con = GetConnection())
                {
                    con.Open();
                    string str = "select * from product where id = @id";
                    MySqlCommand cmd = new MySqlCommand(str, con);
                    cmd.Parameters.AddWithValue("id", id);
                    using (var reader = cmd.ExecuteReader())
                    {

                        reader.Read();
                        product.Id = Convert.ToInt32(reader["Id"]);
                        product.ProductName = Convert.ToString(reader["productname"]);
                        product.SubCategoryId = Convert.ToInt32(reader["subcategory_id"]);
                        product.BrandId = Convert.ToInt32(reader["brand_id"]);
                        product.Gender = Convert.ToInt32(reader["gender"]);
                        product.DefaultImage = Convert.ToString(reader["default_image"]);

                        product.Description = Convert.ToString(reader["description"]);
                        product.SalePrice = Convert.ToDouble(reader["sale_price"]);
                        product.RegularPrice = Convert.ToDouble(reader["regular_price"]);
                        product.Active = Convert.ToBoolean(reader["active"]);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return product;
        }
        public async Task<int[]> InsertProduct(Product product)
        {
            int result = 0, lastindex = -1;
            try
            {
                string extension = Path.GetExtension(product.ImageFile.FileName);
                product.DefaultImage = "product_default_img_" + product.Id.ToString() + DateTime.Now.ToString("yymmssfff") + extension;

                if (!await UploadImage(product.DefaultImage, product.ImageFile))
                {
                    result = -1;
                    return new int[] { result, lastindex };
                }
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "insert into " +
                        "product(productname, brand_id, subcategory_id, gender, default_image, regular_price, sale_price, description, product_tag)" +
                        "values (@productname, @brandid, @subcategoryid, @gender, @defaultimage, @regularprice, @saleprice, @description,@product_tag)";
                    MySqlCommand cmd = new MySqlCommand(str, conn);

                    cmd.Parameters.AddWithValue("productname", product.ProductName);
                    cmd.Parameters.AddWithValue("brandid", product.BrandId);
                    cmd.Parameters.AddWithValue("subcategoryid", product.SubCategoryId);
                    cmd.Parameters.AddWithValue("gender", product.Gender);
                    cmd.Parameters.AddWithValue("defaultimage", product.DefaultImage);
                    cmd.Parameters.AddWithValue("regularprice", product.RegularPrice);
                    cmd.Parameters.AddWithValue("saleprice", product.SalePrice);
                    cmd.Parameters.AddWithValue("description", product.Description);
                    cmd.Parameters.AddWithValue("product_tag", product.ProductTag);

                    result = cmd.ExecuteNonQuery();
                    string getlastindex = "select Last_insert_id()";
                    cmd = new MySqlCommand(getlastindex, conn);
                    lastindex = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch (MySqlException e)
            {
                Console.WriteLine(e.Message);
                result = -1;
            }
            return new int[] { result, lastindex };

        }
        public async Task<int> UpdateProduct(Product product)
        {
            try
            {
                Product oldProduct = GetProductById(product.Id);
                if (product.ImageFile != null)
                {
                    DeleteImage(oldProduct.DefaultImage);
                    string extension = Path.GetExtension(product.ImageFile.FileName);
                    product.DefaultImage = "product_default_img_" + product.Id.ToString() + DateTime.Now.ToString("yymmssfff") + extension;
                    if (!await UploadImage(product.DefaultImage, product.ImageFile))
                    {
                        return -1;
                    }
                }
                else
                {
                    product.DefaultImage = oldProduct.DefaultImage;
                }
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "update product " +
                        "set productname = @productname, " +
                        "brand_id = @brandid, " +
                        "subcategory_id = @subcategoryid, " +
                        "gender = @gender," +
                        "default_image = @defaultimage, " +
                        "regular_price = @regularprice, " +
                        "sale_price = @saleprice, " +
                        "description = @description " +
                        "where id = @id ";
                    MySqlCommand cmd = new MySqlCommand(str, conn);

                    cmd.Parameters.AddWithValue("id", product.Id);
                    cmd.Parameters.AddWithValue("productname", product.ProductName);
                    cmd.Parameters.AddWithValue("brandid", product.BrandId);
                    cmd.Parameters.AddWithValue("subcategoryid", product.SubCategoryId);
                    cmd.Parameters.AddWithValue("gender", product.Gender);
                    cmd.Parameters.AddWithValue("defaultimage", product.DefaultImage);
                    cmd.Parameters.AddWithValue("regularprice", product.RegularPrice);
                    cmd.Parameters.AddWithValue("saleprice", product.SalePrice);
                    cmd.Parameters.AddWithValue("description", product.Description);

                    return cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }

        }
        public int DeleteProduct(int id)
        {
            try
            {
                Product oldProduct = GetProductById(id);
                DeleteImage(oldProduct.DefaultImage);
                using (var conn = GetConnection())
                {
                    conn.Open();
                    List<ProductColorVariant> productColorVariants = GetColorVariantList(id);

                    foreach (var item in productColorVariants)
                    {
                        DeleteProductVariant(id, item.ColorId);
                    }
                    string str = "delete from " +
                        "product " +
                        "where id = @id";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("id", id);
                    int result = cmd.ExecuteNonQuery();


                    //str = "delete from " +
                    //    "product_detail " +
                    //    "where product_id = @id";
                    //cmd = new MySqlCommand(str, conn);
                    //cmd.Parameters.AddWithValue("id", id);


                    //str = "delete from " +
                    //    "product_color_variant " +
                    //    "where product_id = @id";
                    //cmd = new MySqlCommand(str, conn);
                    //cmd.Parameters.AddWithValue("id", id);
                    //cmd.ExecuteNonQuery();

                    return result;
                }
            }
            catch (MySqlException mysqle)
            {
                Console.WriteLine(mysqle.Message);
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        string str = "update " +
                            "product set active = 0 " +
                            "where id = @id";
                        MySqlCommand cmd = new MySqlCommand(str, conn);
                        cmd.Parameters.AddWithValue("id", id);
                        int result = cmd.ExecuteNonQuery();

                        //str = "update " +
                        //    "product_detail set active = 0 " +
                        //    "where product_id = @id";
                        //cmd = new MySqlCommand(str, conn);
                        //cmd.Parameters.AddWithValue("id", id);

                        //str = "update " +
                        //    "product_color_variant set active = 0 " +
                        //    "where product_id = @id";
                        //cmd = new MySqlCommand(str, conn);
                        //cmd.Parameters.AddWithValue("id", id);

                        return result;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return -1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }
        public ProductMasterData GetProductMasterData(int id)
        {
            Product product = GetProductById(id);
            ProductMasterData productMasterData = new ProductMasterData(product);
            List<ProductColorVariant> colorVariants = GetColorVariantList(id);
            productMasterData.ProductVariants = new List<ProductVariantDetail>();

            foreach (var item in colorVariants)
            {
                ProductVariantDetail productVariantDetail = new ProductVariantDetail(item);
                productVariantDetail.ProductDetails = GetProductDetail(id, productVariantDetail.ColorId);
                productMasterData.ProductVariants.Add(productVariantDetail);
            }
            return productMasterData;
        }
        public List<ProductColorVariant> GetColorVariantList(int productId)
        {
            List<ProductColorVariant> variants = new List<ProductColorVariant>();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "select * from product_color_variant where product_id = @productId";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("productId", productId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ProductColorVariant variant = new ProductColorVariant
                            {
                                ProductId = Convert.ToInt32(reader["product_id"]),
                                ColorId = Convert.ToInt32(reader["color_id"]),
                                ProductVariantImage = reader["product_variant_image"].ToString(),
                                ImageBig1 = reader["img_big_1"].ToString(),
                                ImageBig2 = reader["img_big_2"].ToString(),
                                ImageBig3 = reader["img_big_3"].ToString(),
                                ImageBig4 = reader["img_big_4"].ToString(),
                                ImageBig5 = reader["img_big_5"].ToString(),
                                ImageBig6 = reader["img_big_6"].ToString(),
                               
                                Active = Convert.ToBoolean(reader["active"])
                            };
                            variants.Add(variant);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return variants;
        }
        public List<ProductDetail> GetProductDetail(int productId, int colorId)
        {
            List<ProductDetail> list = new List<ProductDetail>();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "select * from product_detail where product_id = @productId and color_id = @colorId";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("productId", productId);
                    cmd.Parameters.AddWithValue("colorId", colorId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ProductDetail detail = new ProductDetail
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                ProductId = Convert.ToInt32(reader["product_id"]),
                                ColorId = Convert.ToInt32(reader["color_id"]),
                                SizeId = Convert.ToInt32(reader["size_id"]),
                                Quantity = Convert.ToInt32(reader["quantity"]),
                                Active = Convert.ToBoolean(reader["active"])
                            };
                            list.Add(detail);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return list;
        }
        public List<ProductColorVariant> GetProductColorVariant(int productId, int colorId)
        {
            List<ProductColorVariant> list = new List<ProductColorVariant>();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "select * from product_color_variant where product_id = @productId and color_id = @colorId";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("productId", productId);
                    cmd.Parameters.AddWithValue("colorId", colorId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ProductColorVariant detail = new ProductColorVariant
                            {
                               
                                ProductId = Convert.ToInt32(reader["product_id"]),
                                ColorId = Convert.ToInt32(reader["color_id"]),
                               // SizeId = Convert.ToInt32(reader["size_id"]),
                                ProductVariantImage=Convert.ToString(reader["product_variant_image"]),
                                ImageBig1=Convert.ToString(reader["img_big_1"]),
                                ImageBig2=Convert.ToString(reader["img_big_2"]),
                                ImageBig3=Convert.ToString(reader["img_big_3"]),
                                ImageBig4=Convert.ToString(reader["img_big_4"]),
                                ImageBig5=Convert.ToString(reader["img_big_5"]),
                                ImageBig6=Convert.ToString(reader["img_big_6"]),
                              
                                // Quantity = Convert.ToInt32(reader["quantity"]),
                                Active = Convert.ToBoolean(reader["active"])
                            };
                            list.Add(detail);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return list;
        }
        


 public ProductDetail GetProductDetail(int id)
        {
            ProductDetail productDetail = new ProductDetail();
            try { 
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "select * from product_detail where id = @id";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        { 
   ProductDetail detail = new ProductDetail
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                ProductId = Convert.ToInt32(reader["product_id"]),
                                ColorId = Convert.ToInt32(reader["color_id"]),
                                SizeId = Convert.ToInt32(reader["size_id"]),
                                Quantity = Convert.ToInt32(reader["quantity"]),
                                Active = Convert.ToBoolean(reader["active"])
                            };
                            return detail;

                      }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
     return productDetail;
        }
        public int GetProductDetailId(int productId, int colorId, int sizeId)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "select id from product_detail where product_id = @productId and color_id = @colorId and size_id = @sizeId";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("productId", productId);
                    cmd.Parameters.AddWithValue("colorId", colorId);
                    cmd.Parameters.AddWithValue("sizeId", sizeId);
                    var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        return reader.GetInt32("id");
                    }
                    
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                return -1;
            }
            return -1;
        }


        public ProductColorVariant GetProductVariantById(int productId, int colorId)
        {
            ProductColorVariant variant = new ProductColorVariant();
            try
            {
                using (var con = GetConnection())
                {
                    con.Open();
                    string str = "select * from product_color_variant where product_id = @productid and color_id = @colorid";
                    MySqlCommand cmd = new MySqlCommand(str, con);
                    cmd.Parameters.AddWithValue("productid", productId);
                    cmd.Parameters.AddWithValue("colorId", colorId);
                    using (var reader = cmd.ExecuteReader())
                    {

                        reader.Read();
                        variant.ProductId = Convert.ToInt32(reader["product_id"]);
                        variant.ColorId = Convert.ToInt32(reader["color_id"]);
                        variant.ProductVariantImage = Convert.ToString(reader["product_variant_image"]);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return variant;
        }
        public async Task<int> InsertColorVariant(ProductColorVariant variant)
        {
            try
            {
               // var arrImageNameFile = new ArrayList();
                string extension = Path.GetExtension(variant.ImageFile.FileName);
                string extension1 = Path.GetExtension(variant.ImageFile1.FileName);
                string extension2 = Path.GetExtension(variant.ImageFile2.FileName);
                string extension3 = Path.GetExtension(variant.ImageFile3.FileName);
                string extension4 = Path.GetExtension(variant.ImageFile4.FileName);
                string extension5 = Path.GetExtension(variant.ImageFile5.FileName);
                string extension6 = Path.GetExtension(variant.ImageFile6.FileName);
                variant.ProductVariantImage = "product_variant_" + variant.ProductId + "_color_" + variant.ColorId.ToString() + DateTime.Now.ToString("yymmssfff") + extension;
                variant.ImageBig1 = "img_big_1_" + variant.ProductId + "_color_" + variant.ColorId.ToString() + DateTime.Now.ToString("yymmssfff") + extension1;
                variant.ImageBig2 = "img_big_2_" + variant.ProductId + "_color_" + variant.ColorId.ToString() + DateTime.Now.ToString("yymmssfff") + extension2;
                variant.ImageBig3 = "img_big_3_" + variant.ProductId + "_color_" + variant.ColorId.ToString() + DateTime.Now.ToString("yymmssfff") + extension3;
                variant.ImageBig4 = "img_big_4_" + variant.ProductId + "_color_" + variant.ColorId.ToString() + DateTime.Now.ToString("yymmssfff") + extension4;
                variant.ImageBig5 = "img_big_5_" + variant.ProductId + "_color_" + variant.ColorId.ToString() + DateTime.Now.ToString("yymmssfff") + extension5;
                variant.ImageBig6 = "img_big_6_" + variant.ProductId + "_color_" + variant.ColorId.ToString() + DateTime.Now.ToString("yymmssfff") + extension6;
                /* arrImageNameFile.Add(new ProductImageFile(variant.ProductVariantImage,variant.ImageFile));
                 arrImageNameFile.Add(new ProductImageFile(variant.ImageBig1,variant.ImageFile1));
                 arrImageNameFile.Add(new ProductImageFile(variant.ImageBig2,variant.ImageFile2));
                 arrImageNameFile.Add(new ProductImageFile(variant.ImageBig3,variant.ImageFile3));
                 arrImageNameFile.Add(new ProductImageFile(variant.ImageBig4,variant.ImageFile4));
                 arrImageNameFile.Add(new ProductImageFile(variant.ImageBig5,variant.ImageFile5));
                 arrImageNameFile.Add(new ProductImageFile(variant.ImageBig6,variant.ImageFile6));
                 ProductImageFile[] array = arrImageNameFile.ToArray(typeof(ProductImageFile)) as ProductImageFile[];
                 if (!await UploadImage1(array))
                 {
                     return -1;
                 }
                */
                if (!await UploadImage1(variant.ProductVariantImage, variant.ImageBig1, variant.ImageBig2, variant.ImageBig3, variant.ImageBig4, variant.ImageBig5, variant.ImageBig6, variant.ImageFile, variant.ImageFile1, variant.ImageFile2, variant.ImageFile3, variant.ImageFile4, variant.ImageFile5, variant.ImageFile6))
                {
                    return -1;
                }

                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "insert into " +
                        "product_color_variant (product_id, color_id, product_variant_image, img_big_1, img_big_2, img_big_3, img_big_4, img_big_5, img_big_6)" +
                        "values (@productId, @colorId, @variantImg,@img_1,@img_2,@img_3,@img_4,@img_5,@img_6)";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("productId", variant.ProductId);
                    cmd.Parameters.AddWithValue("colorId", variant.ColorId);
                    cmd.Parameters.AddWithValue("variantImg", variant.ProductVariantImage);
                    cmd.Parameters.AddWithValue("img_1", variant.ImageBig1);
                    cmd.Parameters.AddWithValue("img_2", variant.ImageBig2);
                    cmd.Parameters.AddWithValue("img_3", variant.ImageBig3);
                    cmd.Parameters.AddWithValue("img_4", variant.ImageBig4);
                    cmd.Parameters.AddWithValue("img_5", variant.ImageBig5);
                    cmd.Parameters.AddWithValue("img_6", variant.ImageBig6);
                   
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }
        public async Task<int> UpdateProductVariant(ProductColorVariant variant)
        {
            try
            {
                string oldVariantImage = GetProductVariantById(variant.ProductId, variant.ColorId).ProductVariantImage;
                if (variant.ImageFile != null)
                {
                    DeleteImage(oldVariantImage);
                    string extension = Path.GetExtension(variant.ImageFile.FileName);
                    variant.ProductVariantImage = "product_variant_" + variant.ProductId + "_color_" + variant.ColorId.ToString() + DateTime.Now.ToString("yymmssfff") + extension;
                    if (!await UploadImage(variant.ProductVariantImage, variant.ImageFile))
                    {
                        return -1;
                    }
                }
                else
                {
                    variant.ProductVariantImage = oldVariantImage;
                }
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "update product_color_variant " +
                        "set product_variant_image = @colorImage " +
                        "where product_id = @productId and color_id = @colorId";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("productId", variant.ProductId);
                    cmd.Parameters.AddWithValue("colorId", variant.ColorId);
                    cmd.Parameters.AddWithValue("colorImage", variant.ProductVariantImage);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }
        public int DeleteProductVariant(int productId, int colorId)
        {
            try
            {
                var oldVariant = GetColorVariantList(productId).Where(v => v.ColorId == colorId).FirstOrDefault();
                using (var conn = GetConnection())
                {
                    DeleteImage(oldVariant.ProductVariantImage);
                    conn.Open();
                    string str = "delete from " +
                        "product_color_variant " +
                        "where product_id = @productId and color_id = @colorId";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("productId", productId);
                    cmd.Parameters.AddWithValue("colorId", colorId);
                    int result = cmd.ExecuteNonQuery();
                    str = "delete from " +
                        "product_detail " +
                        "where product_id = @productId and color_id = @colorId";
                    cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("productId", productId);
                    cmd.Parameters.AddWithValue("colorId", colorId);

                    cmd.ExecuteNonQuery();
                    return result;
                }
            }
            catch (MySqlException mysqle)
            {
                Console.WriteLine(mysqle.Message);
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        string str = "update " +
                            "product_color_variant set active = 0 " +
                            "where product_id = @productId and color_id = @colorId";
                        MySqlCommand cmd = new MySqlCommand(str, conn);
                        cmd.Parameters.AddWithValue("productId", productId);
                        cmd.Parameters.AddWithValue("colorId", colorId);
                        int result = cmd.ExecuteNonQuery();

                        str = "update " +
                            "product_detail set active = 0 " +
                            "where product_id = @productId and color_id = @colorId";
                        cmd = new MySqlCommand(str, conn);
                        cmd.Parameters.AddWithValue("productId", productId);
                        cmd.Parameters.AddWithValue("colorId", colorId);

                        return result;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return -1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }
        public int InsertProductDetail(ProductDetail productDetail)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    ProductDetail detail = GetProductDetail(productDetail.ProductId, productDetail.ColorId)
                        .Where<ProductDetail>(item => item.SizeId == productDetail.SizeId)
                        .FirstOrDefault();
                    if (detail != null)
                    {
                        detail.Quantity += productDetail.Quantity;
                        UpdateProductDetail(detail);
                        return 1;
                    }
                    conn.Open();
                    string str = "insert into " +
                        "product_detail (product_id, color_id, size_id, quantity)" +
                        "values (@productId, @colorId, @sizeId, @quantity)";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("productId", productDetail.ProductId);
                    cmd.Parameters.AddWithValue("colorId", productDetail.ColorId);
                    cmd.Parameters.AddWithValue("sizeId", productDetail.SizeId);
                    cmd.Parameters.AddWithValue("quantity", productDetail.Quantity);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }
      /*  public int InsertProductVariant(ProductColorVariant variant)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    ProductDetail detail = GetProductVariant(variant.ProductId, variant.ColorId)
                        .Where<ProductColorVariant>(item => item.ColorId == variant.ColorId)
                        .FirstOrDefault();
                    if (detail != null)
                    {
                        detail.Quantity += productDetail.Quantity;
                        UpdateProductDetail(detail);
                        return 1;
                    }
                    conn.Open();
                    string str = "insert into " +
                        "product_detail (product_id, color_id, size_id, quantity)" +
                        "values (@productId, @colorId, @sizeId, @quantity)";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("productId", productDetail.ProductId);
                    cmd.Parameters.AddWithValue("colorId", productDetail.ColorId);
                    cmd.Parameters.AddWithValue("sizeId", productDetail.SizeId);
                    cmd.Parameters.AddWithValue("quantity", productDetail.Quantity);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        } */
        public int UpdateProductDetail(ProductDetail productDetail)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "update " +
                        "product_detail set quantity = @quantity " +
                        "where id = @id";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("id", productDetail.Id);
                    cmd.Parameters.AddWithValue("quantity", productDetail.Quantity);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }
        public int DeleteProductDetail(int id)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "delete from " +
                        "product_detail " +
                        "where id = @id";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("id", id);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException mysqle)
            {
                Console.WriteLine(mysqle.Message);
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        string str = "update " +
                            "product_detail set active = 0 " +
                            "where id = @id";
                        MySqlCommand cmd = new MySqlCommand(str, conn);
                        cmd.Parameters.AddWithValue("id", id);
                        return cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return -1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }
        //Category CRUD - start
        public List<CategoryMasterModel> GetCategoryMasters()
        {
            List<CategoryMasterModel> list = new List<CategoryMasterModel>();
            foreach (var cate in GetCategories())
            {
                list.Add(new CategoryMasterModel
                {
                    Id = cate.Id,
                    CategoryName = cate.CategoryName,
                    SubCategoryList = GetSubCategoriesByCateId(cate.Id)
                });
            }
            return list;

        }
        public CategoryMasterModel GetCategoryMasterDataById(int id)
        {
            CategoryMasterModel result = new CategoryMasterModel();
            Category category = GetCategoryById(id);
            result.Id = category.Id;
            result.CategoryName = category.CategoryName;
            result.Active = category.Active;
            result.SubCategoryList = GetSubCategoriesByCateId(id);
            return result;
        }
        public List<Category> GetCategories()
        {
            List<Category> list = new List<Category>();
            using (var conn = GetConnection())
            {
                conn.Open();
                string str = "select * from category";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Category
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            CategoryName = Convert.ToString(reader["categoryName"]),
                            Active = Convert.ToBoolean(reader["active"])
                        });
                    }
                }
            }
            return list;
        }

        public Category GetCategoryById(int id)
        {
            Category category = new Category();
            using (var conn = GetConnection())
            {
                conn.Open();
                string str = "select * from category where id = @id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    category.Id = Convert.ToInt32(reader["id"]);
                    category.CategoryName = Convert.ToString(reader["categoryName"]);
                    category.Active = Convert.ToBoolean(reader["active"]);
                }
            }
            return category;
        }
        public Dictionary<string, int> InsertCategory(Category category)
        {
            Dictionary<string, int> resultDict = new Dictionary<string, int>();
            int result = 0, lastIdx = -1;
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "insert into " +
                        "category(categoryName)" +
                        "values (@categoryName)";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("categoryName", category.CategoryName);
                    result = cmd.ExecuteNonQuery();
                    string getlastindex = "select Last_insert_id()";
                    cmd = new MySqlCommand(getlastindex, conn);
                    lastIdx = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch (MySqlException e)
            {
                Console.WriteLine(e.Message);
                result = -1;
            }
            resultDict.Add("result", result);
            resultDict.Add("id", lastIdx);
            return resultDict;
        }
        public int UpdateCategory(Category category)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "update category " +
                        "set categoryname = @categoryName " +
                        "where id = @id";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("categoryName", category.CategoryName);
                    cmd.Parameters.AddWithValue("id", category.Id);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return -1;
            }
        }
        public int DeleteCategory(int id)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "delete " +
                        "from category " +
                        "where id = @id";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("id", id);
                    int result = cmd.ExecuteNonQuery();

                    str = "delete " +
                        "from subcategory " +
                        "where category_id = @id";
                    cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.ExecuteNonQuery();
                    return result;
                }
            }
            catch (MySqlException mysqle)
            {
                Console.WriteLine(mysqle);
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        string str = "update category " +
                            "set active = 0 " +
                            "where id = @id";
                        MySqlCommand cmd = new MySqlCommand(str, conn);
                        cmd.Parameters.AddWithValue("id", id);
                        int result = cmd.ExecuteNonQuery();

                        str = "update subcategory " +
                            "set active = 0 " +
                            "where id = @id";
                        cmd = new MySqlCommand(str, conn);
                        cmd.Parameters.AddWithValue("id", id);
                        cmd.ExecuteNonQuery();

                        return result;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return -1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }
        public List<SubCategory> GetSubCategoriesByCateId(int id)
        {
            List<SubCategory> list = new List<SubCategory>();
            using (var conn = GetConnection())
            {
                conn.Open();
                string str = "select * from SubCategory where category_id = @id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new SubCategory
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            SubCategoryName = Convert.ToString(reader["subcategory_name"]),
                            CategoryId = Convert.ToInt32(reader["category_id"]),
                            Active = Convert.ToBoolean(reader["active"])
                        });
                    }
                }
            }
            return list;
        }
        public List<SubCategory> GetSubCategories()
        {
            List<SubCategory> list = new List<SubCategory>();
            using (var conn = GetConnection())
            {
                conn.Open();
                string str = "select * from SubCategory";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new SubCategory
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            SubCategoryName = Convert.ToString(reader["subcategory_name"]),
                            CategoryId = Convert.ToInt32(reader["category_id"]),
                            Active = Convert.ToBoolean(reader["active"])
                        });
                    }
                }
            }
            return list;
        }
        public SubCategory GetSubCategoryById(int id)
        {
            SubCategory subCategory = new SubCategory();
            using (var conn = GetConnection())
            {
                conn.Open();
                string str = "select * from SubCategory where id = @id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();

                    subCategory.Id = Convert.ToInt32(reader["id"]);
                    subCategory.SubCategoryName = Convert.ToString(reader["subcategory_name"]);
                    subCategory.CategoryId = Convert.ToInt32(reader["category_id"]);
                    subCategory.Active = Convert.ToBoolean(reader["active"]);

                }
            }
            return subCategory;
        }
        public int InsertSubCategory(SubCategory subCategory)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "insert into subcategory(subcategory_name, category_id) " +
                        "values(@subcategoryName, @categoryId)";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("subcategoryName", subCategory.SubCategoryName);
                    cmd.Parameters.AddWithValue("categoryId", subCategory.CategoryId);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return -1;
            }
        }
        public int UpdateSubCategory(SubCategory subCategory)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "update subcategory " +
                        "set subcategory_name = @subcategoryName " +
                        "where id = @id";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("subcategoryName", subCategory.SubCategoryName);
                    cmd.Parameters.AddWithValue("id", subCategory.Id);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return -1;
            }
        }
        public int DeleteSubCategory(int id)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "delete " +
                        "from subcategory " +
                        "where id = @id";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("id", id);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException mysqle)
            {
                Console.WriteLine(mysqle);
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        string str = "update subcategory " +
                            "set active = 0 " +
                            "where id = @id";
                        MySqlCommand cmd = new MySqlCommand(str, conn);
                        cmd.Parameters.AddWithValue("id", id);
                        return cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return -1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }
        //Category CRUD - end

        //Brand CRUD - start
        public List<Brand> GetBrands()
        {
            List<Brand> list = new List<Brand>();
            using (var conn = GetConnection())
            {
                conn.Open();
                string str = "select * from brand";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Brand
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            BrandName = Convert.ToString(reader["brand_name"]),
                            Active = Convert.ToBoolean(reader["active"])
                        });
                    }
                }
            }
            return list;
        }
        public Brand GetBrandById(int id)
        {
            Brand brand = new Brand();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "select * from brand where id = @id";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        brand.Id = Convert.ToInt32(reader["id"]);
                        brand.BrandName = Convert.ToString(reader["brand_name"]);
                        brand.Active = Convert.ToBoolean(reader["active"]);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return brand;
        }
        public int InsertBrand(Brand brand)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "insert into " +
                        "brand(id, brand_name) " +
                        "value(@id, @brandName)";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("id", brand.Id);
                    cmd.Parameters.AddWithValue("brandName", brand.BrandName);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }
        public int UpdateBrand(Brand brand)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "update brand " +
                        "set brand_name = @brandName " +
                        "where id = @id";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("id", brand.Id);
                    cmd.Parameters.AddWithValue("brandName", brand.BrandName);
                    cmd.Parameters.AddWithValue("brandName", brand.BrandName);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }

        public int DeleteBrand(int id)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "delete " +
                        "from brand " +
                        "where id = @id";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("id", id);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException mysqle)
            {
                Console.WriteLine(mysqle.Message);
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        string str = "update brand " +
                            "set active = 0 " +
                            "where id = @id";
                        MySqlCommand cmd = new MySqlCommand(str, conn);
                        cmd.Parameters.AddWithValue("id", id);
                        return cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return -1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }
        //Brand CRUD - end

        //Size CRUD - start
        public List<Size> GetSizes()
        {
            List<Size> list = new List<Size>();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "select * from size";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Size
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                SizeName = Convert.ToString(reader["size_name"]),
                                Active = Convert.ToBoolean(reader["active"])
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return list;
        }
        public Size GetSizeById(int id)
        {
            Size size = new Size();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "select * from size where id = @id";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        size.Id = Convert.ToInt32(reader["id"]);
                        size.SizeName = Convert.ToString(reader["size_name"]);
                        size.Active = Convert.ToBoolean(reader["active"]);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return size;
        }
        public int InsertSize(Size size)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "insert into " +
                        "size(id, size_name) " +
                        "value(@id, @sizeName)";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("id", size.Id);
                    cmd.Parameters.AddWithValue("sizeName", size.SizeName);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }
        public int UpdateSize(Size size)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "update size " +
                        "set size_name = @sizeName " +
                        "where id = @id";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("id", size.Id);
                    cmd.Parameters.AddWithValue("sizeName", size.SizeName);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }

        public int DeleteSize(int id)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "delete " +
                        "from size " +
                        "where id = @id";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("id", id);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException mysqle)
            {
                Console.WriteLine(mysqle.Message);
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        string str = "update size " +
                            "set active = 0 " +
                            "where id = @id";
                        MySqlCommand cmd = new MySqlCommand(str, conn);
                        cmd.Parameters.AddWithValue("id", id);
                        return cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return -1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }
        //Size CRUD - end

        //Color CRUD - start
        public List<Color> GetColors()
        {
            List<Color> listColor = new List<Color>();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "select * from color";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listColor.Add(new Color
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                ColorName = Convert.ToString(reader["color_name"]),
                                ColorImage = Convert.ToString(reader["color_image"]),
                                Active = Convert.ToBoolean(reader["active"])
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return listColor;
        }
        public Color GetColorById(int id)
        {
            Color color = new Color();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "select * from color where id = @id";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        color.Id = Convert.ToInt32(reader["id"]);
                        color.ColorName = Convert.ToString(reader["color_name"]);
                        color.ColorImage = Convert.ToString(reader["color_image"]);
                        color.Active = Convert.ToBoolean(reader["active"]);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return color;

        }
        public async Task<int> InsertColorAsync(Color color)
        {
            try
            {
                string extension = Path.GetExtension(color.ImageFile.FileName);
                color.ColorImage = "color_" + color.ColorName + DateTime.Now.ToString("yymmssfff") + extension;

                if (!await UploadImage(color.ColorImage, color.ImageFile))
                {
                    return -1;
                }

                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "insert into " +
                        "color (color_name, color_image)" +
                        "values (@colorName, @colorImage)";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("colorName", color.ColorName);
                    cmd.Parameters.AddWithValue("colorImage", color.ColorImage);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }

        public async Task<int> UpdateColorAsync(Color color)
        {
            try
            {
                Color oldColor = GetColorById(color.Id);
                if (color.ImageFile != null)
                {
                    DeleteImage(oldColor.ColorImage);
                    string extension = Path.GetExtension(color.ImageFile.FileName);
                    color.ColorImage = "color_" + color.ColorName + DateTime.Now.ToString("yymmssfff") + extension;
                    if (!await UploadImage(color.ColorImage, color.ImageFile))
                    {
                        return -1;
                    }
                }
                else
                {
                    color.ColorImage = oldColor.ColorImage;
                }
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "update color " +
                        "set color_name = @colorName, color_image = @colorImage " +
                        "where id = @id";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("colorName", color.ColorName);
                    cmd.Parameters.AddWithValue("id", color.Id);
                    cmd.Parameters.AddWithValue("colorImage", color.ColorImage);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }
        public int DeleteColor(int id)
        {
            Color color = GetColorById(id);
            try
            {
                DeleteImage(color.ColorImage);
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string str = "delete " +
                        "from color " +
                        "where id = @id";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("id", color.Id);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (MySqlException mysqle)
            {
                Console.WriteLine(mysqle);
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        string str = "update color " +
                            "set active = 0 " +
                            "where id = @id";
                        MySqlCommand cmd = new MySqlCommand(str, conn);
                        cmd.Parameters.AddWithValue("id", color.Id);
                        return cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return -1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }
        //Color CRUD - end 

        //image upload method - start
       // public async Task<bool> UploadImage1(ProductImageFile []  a)
        public async Task<bool> UploadImage1(string imageName, string imageName1, string imageName2, string imageName3, string imageName4, string imageName5, string imageName6, IFormFile imageFile, IFormFile imageFile1, IFormFile imageFile2, IFormFile imageFile3, IFormFile imageFile4, IFormFile imageFile5, IFormFile imageFile6)
        {
            try
            {
                string rootPath = _rootPath;
               // for (int i = 0; i <= a.Length; i++)
               // {
                 //   string imageName = "imageName" + Convert.ToString(i);

                    string path = Path.Combine(rootPath + "/Image/", imageName);
                      string path1 = Path.Combine(rootPath + "/Image/", imageName1);
                      string path2 = Path.Combine(rootPath + "/Image/", imageName2);
                      string path3 = Path.Combine(rootPath + "/Image/", imageName3);
                      string path4 = Path.Combine(rootPath + "/Image/", imageName4);
                      string path5 = Path.Combine(rootPath + "/Image/", imageName5);
                      string path6 = Path.Combine(rootPath + "/Image/", imageName6);

                

                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);

                    }
                
                using (var fileStream = new FileStream(path1, FileMode.Create))
                {
                    await imageFile1.CopyToAsync(fileStream);

                }
                using (var fileStream = new FileStream(path2, FileMode.Create))
                {
                    await imageFile2.CopyToAsync(fileStream);

                }
                using (var fileStream = new FileStream(path3, FileMode.Create))
                {
                    await imageFile3.CopyToAsync(fileStream);

                }
                using (var fileStream = new FileStream(path4, FileMode.Create))
                {
                    await imageFile4.CopyToAsync(fileStream);

                }
                using (var fileStream = new FileStream(path5, FileMode.Create))
                {
                    await imageFile5.CopyToAsync(fileStream);

                }
                using (var fileStream = new FileStream(path6, FileMode.Create))
                {
                    await imageFile6.CopyToAsync(fileStream);

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            return true;
        }
        public async Task<bool> UploadImage(string imageName, IFormFile imageFile)
        {
            try
            {
                string rootPath = _rootPath;
                string path = Path.Combine(rootPath + "/Image/", imageName);
                using (var fileStream = new FileStream(path, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                  
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            return true;
        }
        public bool DeleteImage(string imageName)
        {
            try
            {
                string rootPath = _rootPath;
                string absoluteFilePath = Path.Combine(rootPath + "/image/", imageName);
                FileInfo file = new FileInfo(absoluteFilePath);
                if (file.Exists)
                {
                    file.Delete();
                }
                else return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            return true;
        }
        public bool UpdateImageName(string oldName, string newName)
        {
            try
            {
                string rootPath = _rootPath;
                string oldFilePath = Path.Combine(rootPath + "/image/", oldName);
                string newFilePath = Path.Combine(rootPath + "/image/", newName);
                File.Move(oldFilePath, newFilePath);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            return true;
        }
        //image upload method - end




        /*public int InsertIn4(User usr)
        {
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open(); 
                var str = "insert into user (fullname,email,password) values(@FullName, @EMail, @PAssword)";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("FullName", usr.fullname);
                cmd.Parameters.AddWithValue("EMail", usr.email);
                cmd.Parameters.AddWithValue("PAssword", usr.password);
                return (cmd.ExecuteNonQuery());
            }
        }*/
        public int InsertIn4(User usr)
        {
            //checking if user already exist
            if (!IsUserExist(usr.Email))
            {
                using (MySqlConnection conn = GetConnection())
                {
                    conn.Open();
                    var str = "insert into user (fullname,email,password) values(@FullName, @EMail, @PAssword)";
                    MySqlCommand cmd = new MySqlCommand(str, conn);
                    cmd.Parameters.AddWithValue("FullName", usr.Fullname);
                    cmd.Parameters.AddWithValue("EMail", usr.Fullname);
                    cmd.Parameters.AddWithValue("PAssword", usr.Password);
                    return (cmd.ExecuteNonQuery());
                }
            }
            else
            {
                return 0;
            }
        }
        private bool IsUserExist(string email)
        {
            bool IsUserExist = false;
            string str = "select * from user where email=@email";
            using (MySqlConnection conn = GetConnection())
            {
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("@email", email);
                MySqlDataAdapter sda = new MySqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                sda.Fill(dt);
                conn.Open();
                int i = cmd.ExecuteNonQuery();
                conn.Close();
                if (dt.Rows.Count > 0)
                {
                    IsUserExist = true;
                }
            }
            return IsUserExist;
        }
        public User LogIn(string email, string password)
        {
            User user = new User();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                string str = "select * from user where email=@email and password=@password";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("email", email);
                cmd.Parameters.AddWithValue("password", password);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            user.Fullname = reader["fullname"].ToString();
                            user.ID = Convert.ToInt32(reader["id"]);
                        }
                        reader.Close();
                        return user;
                    }
                }
                conn.Close();
                //phần mới thêm
            }
            return null;
        }




        public List<object> GetColorOfProduct()
        {
            List<object> list = new List<object>();


            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();

                string str = "select distinct ProductName, Color_name, Size_name  from Product p,Color c, Product_Color_variant v, Product_detail d, Size s " +
                "where p.Id= d.Product_Id and d.Color_Id=c.Id and d.Size_Id=s.Id";


                MySqlCommand cmd = new MySqlCommand(str, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var ob = new
                        {
                            Productname = reader["ProductName"].ToString(),
                            Colorname = reader["Color_name"].ToString(),
                            Sizename = reader["Size_name"].ToString()
                        };

                        list.Add(ob);
                    }

                    reader.Close();
                }

                conn.Close();

            }
            return list;


        }
        public List<Color> GetColorsOfProduct(int product_id)
        {
            //int i = 0;
            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct c.id as color_id, color_name , color_image from color c, product p, product_detail d where c.id=d.color_id and p.id=d.product_id and p.id=@id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", product_id);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            ColorName = reader["color_name"].ToString(),
                            ColorImage = reader["color_image"].ToString(),
                            Id = Convert.ToInt32(reader["color_id"]),


                        });
                    }
                }
            }
            return list;
        }


        public List<Product> GetProducts(int IdColor)
        {
            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct p.Id, ProductName from Product p,Color c,Product_detail a where p.Id= a.Product_Id and " +
                    " a.Color_Id=@IdColor";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("IdColor", IdColor);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            ProductName = reader["ProductName"].ToString(),


                        });
                    }
                }
            }
            return list;
        }
        public List<Product> GetProductNewCate(int id)
        {
            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select * from Product p, subcategory s where Product_new = 1 and p.subcategory_id= s.id and s.category_id = @id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),
                            ProductNew = Convert.ToBoolean(reader["Product_new"]),

                        });
                    }
                }
            }
            return list;
        }
        public List<Product> GetProductNewSubCate(int id)
        {
            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select * from Product where Product_new = 1 and subcategory_id = @id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),
                            ProductNew = Convert.ToBoolean(reader["Product_new"]),

                        });
                    }
                }
            }
            return list;
        }
        public List<Product> GetProduct()
        {
            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select * from Product";
                MySqlCommand cmd = new MySqlCommand(str, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            DefaultImage = reader["default_image"].ToString(),
                            ProductNew = Convert.ToBoolean(reader["Product_new"]),

                        });
                    }
                }
            }
            return list;
        }


        public List<Product> GetProductsSub(int page, int id)

        {

            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT distinct p.id as product_id, brand_id, ProductName,sale_price,regular_price, default_image FROM product p " +
                    "WHERE p.subcategory_id= @id order by p.id limit @page, 9";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("page", page);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["product_id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }
        public List<Product> GetProductsSizeSub(int page,int size_id, int subcate_id)

        {

            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT distinct p.id as product_id, brand_id, ProductName,sale_price,regular_price, default_image " +
                    "from product p , product_detail d where p.subcategory_id = @subcate_id and p.id= d.product_id and d.size_id = @size_id limit @page, 9";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("size_id", size_id);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);
                cmd.Parameters.AddWithValue("page", page);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["product_id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }

        public List<Product> GetProductsRelateSub(int product_id)

        {

            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select * from product where id!= @product_id and subcategory_id = (select subcategory_id from product where id = @product_id) limit 4";


                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("product_id", product_id);


                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }
        public List<Color> GetColorBySubId(int subcate_id, int product_id)

        {

            //int i = 0;
            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct color_id , color_image from color c , product p , product_detail d " +
                    "where p.id = d.product_id and c.id = d.color_id and p.subcategory_id = @subcate_id and p.id =@product_id order by color_id desc";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);
                cmd.Parameters.AddWithValue("product_id", product_id);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = Convert.ToString(reader["color_image"]),

                        });
                    }
                }
            }
            return list;
        }
        public List<ProductColorVariant> GetVariantBySubId(int product_id)

        {

            //int i = 0;
            List<ProductColorVariant> list = new List<ProductColorVariant>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select color_id ,product_variant_image from product_color_variant  where product_id = @product_id order by color_id desc";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("product_id", product_id);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new ProductColorVariant()
                        {
                            ColorId = Convert.ToInt32(reader["color_id"]),
                            ProductVariantImage = Convert.ToString(reader["product_variant_image"])

                        });
                    }
                }
            }
            return list;
        }
        public List<Color> GetColorBySizeSub(int subcate_id, int product_id, int size_id)

        {

            //int i = 0;
            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct color_id , color_image from color c , product p , product_detail d " +
                    " where p.subcategory_id = @subcate_id and p.id= d.product_id and d.size_id = @size_id and p.id =@product_id and c.id = d.color_id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);
                cmd.Parameters.AddWithValue("product_id", product_id);
                cmd.Parameters.AddWithValue("size_id", size_id);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = Convert.ToString(reader["color_image"]),

                        });
                    }
                }
            }
            return list;
        }
        public List<Color> GetColorPopularSub(int subcate_id, int product_id)

        {

            //int i = 0;
            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct color_id , color_image from color c , product p , product_detail d " +
                    "where p.active =1 and p.id = d.product_id and c.id = d.color_id and p.subcategory_id = @subcate_id and p.id =@product_id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);
                cmd.Parameters.AddWithValue("product_id", product_id);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = Convert.ToString(reader["color_image"]),

                        });
                    }
                }
            }
            return list;
        }
        public List<Product> GetProductsPopularSub(int page, int id)

        {

            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT *FROM product WHERE active = 1 and subcategory_id = @id  limit @page, 9";
                ;
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("page", page);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }
        public List<ProductColorSub> GetProductPopularSubCate(int page, int id)
        {
            //int i = 0;
            List<ProductColorSub> list = new List<ProductColorSub>();
            foreach (var pro in GetProductsPopularSub(page, id))
            {
                list.Add(new ProductColorSub()
                {
                    Id = pro.Id,
                    BrandId = pro.BrandId,
                    ProductName = pro.ProductName,
                    SalePrice = pro.SalePrice,
                    RegularPrice = pro.RegularPrice,
                    DefaultImage = pro.DefaultImage,
                    productcolorMaster = GetColorPopularSub(id, pro.Id)
                });
            }

            return list;
        }

        //New Product start
        public List<Color> GetColorNewSub(int subcate_id, int product_id)

        {


            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct color_id , color_image from color c , product p , product_detail d " +
                    "where p.product_new = 1 and p.id = d.product_id and c.id = d.color_id and p.subcategory_id = @subcate_id and p.id =@product_id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);
                cmd.Parameters.AddWithValue("product_id", product_id);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = Convert.ToString(reader["color_image"]),

                        });
                    }
                }
            }
            return list;
        }
        public List<Product> GetProductsNewSub(int page, int subcate_id)

        {

            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT *FROM product WHERE product_new = 1 and subcategory_id = @subcate_id  limit @page, 9";
                ;
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);
                cmd.Parameters.AddWithValue("page", page);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }
        public List<ProductColorSub> GetProductNewSub(int page, int subcate_id)
        {
            //int i = 0;
            List<ProductColorSub> list = new List<ProductColorSub>();
            foreach (var pro in GetProductsNewSub(page, subcate_id))
            {
                list.Add(new ProductColorSub()
                {
                    Id = pro.Id,
                    BrandId = pro.BrandId,
                    ProductName = pro.ProductName,
                    SalePrice = pro.SalePrice,
                    RegularPrice = pro.RegularPrice,
                    DefaultImage = pro.DefaultImage,
                    productcolorMaster = GetColorNewSub(subcate_id, pro.Id)
                });
            }

            return list;
        }
        public int GetAllProductsNewSub(int id)
        {
            //int i = 0;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT count(*) FROM product  WHERE subcategory_id= @id and product_new =1";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);
                int count = Convert.ToInt32(cmd.ExecuteScalar());


                return count;
            }

        }
        //End New
        // ASC
        public List<Color> GetColorASCSub(int subcate_id, int product_id)

        {


            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct color_id , color_image from color c , product p , product_detail d " +
                    "where p.id = d.product_id and c.id = d.color_id and p.subcategory_id = @subcate_id and p.id =@product_id order by p.sale_price asc";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);
                cmd.Parameters.AddWithValue("product_id", product_id);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = Convert.ToString(reader["color_image"]),

                        });
                    }
                }
            }
            return list;
        }
        public List<Product> GetProductsASCSub(int page, int subcate_id)

        {

            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT *FROM product WHERE  subcategory_id = @subcate_id order by sale_price asc limit @page, 9";
                ;
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);
                cmd.Parameters.AddWithValue("page", page);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }
        public List<ProductColorSub> GetProductASCSub(int page, int subcate_id)
        {
            //int i = 0;
            List<ProductColorSub> list = new List<ProductColorSub>();
            foreach (var pro in GetProductsASCSub(page, subcate_id))
            {
                list.Add(new ProductColorSub()
                {
                    Id = pro.Id,
                    BrandId = pro.BrandId,
                    ProductName = pro.ProductName,
                    SalePrice = pro.SalePrice,
                    RegularPrice = pro.RegularPrice,
                    DefaultImage = pro.DefaultImage,
                    productcolorMaster = GetColorASCSub(subcate_id, pro.Id)
                });
            }

            return list;
        }
        public int GetAllProductsASCSub(int id)
        {
            //int i = 0;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT count(*) FROM product  WHERE subcategory_id= @id ";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);
                int count = Convert.ToInt32(cmd.ExecuteScalar());


                return count;
            }

        }

        //End ASC
        //DESC
        public List<Color> GetColorDESCSub(int subcate_id, int product_id)

        {


            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct color_id , color_image from color c, Product p,product_detail d where c.id = d.color_id and p.id=@product_id and subcategory_id = @subcate_id and p.id = d.product_id and d.color_id = c.id order by sale_price desc";
                ;
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);
                cmd.Parameters.AddWithValue("product_id", product_id);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = Convert.ToString(reader["color_image"]),

                        });
                    }
                }
            }
            return list;
        }
        public List<Product> GetProductsDESCSub(int page, int subcate_id)

        {

            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT * from Product where subcategory_id = @subcate_id  order by sale_price desc limit @page, 9";
                ;
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);
                cmd.Parameters.AddWithValue("page", page);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }
        public List<ProductColorSub> GetProductDESCSub(int page, int subcate_id)
        {
            //int i = 0;
            List<ProductColorSub> list = new List<ProductColorSub>();
            foreach (var pro in GetProductsDESCSub(page, subcate_id))
            {
                list.Add(new ProductColorSub()
                {
                    Id = pro.Id,
                    BrandId = pro.BrandId,
                    ProductName = pro.ProductName,
                    SalePrice = pro.SalePrice,
                    RegularPrice = pro.RegularPrice,
                    DefaultImage = pro.DefaultImage,
                    productcolorMaster = GetColorDESCSub(subcate_id, pro.Id)
                });
            }

            return list;
        }

     
        public int GetAllProductsDESCSub(int id)
        {
            //int i = 0;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT count(*) from Product where subcategory_id = @id ";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);
                int count = Convert.ToInt32(cmd.ExecuteScalar());


                return count;
            }

        }
        //End DESC
        //BestSeller
        public List<Color> GetColorBestSellerSub(int subcate_id, int product_id)

        {


            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT distinct color_id, color_image from color c,Product p, Product_detail d, order_detail o, " +
                    " subcategory s where p.id = @product_id and c.id=d.color_id and p.Id = d.Product_Id and d.Id = o.Product_detail_Id and p.subcategory_id =@subcate_id group by Productname order by count(o.Product_detail_Id) desc";

                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);
                cmd.Parameters.AddWithValue("product_id", product_id);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = Convert.ToString(reader["color_image"]),

                        });
                    }
                }
            }
            return list;
        }
        public List<Product> GetProductsBestSellerSub(int page, int subcate_id)

        {

            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT * FROM Product p, Product_detail d, order_detail o " +
                    "where p.Id = d.Product_Id and d.Id = o.Product_detail_Id and p.subcategory_id =@subcate_id order by o.quantity desc limit @page, 9";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);
                cmd.Parameters.AddWithValue("page", page);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }
        public List<ProductColorSub> GetProductBestSellerSub(int page, int subcate_id)
        {
            //int i = 0;
            List<ProductColorSub> list = new List<ProductColorSub>();
            foreach (var pro in GetProductsBestSellerSub(page, subcate_id))
            {
                list.Add(new ProductColorSub()
                {
                    Id = pro.Id,
                    BrandId = pro.BrandId,
                    ProductName = pro.ProductName,
                    SalePrice = pro.SalePrice,
                    RegularPrice = pro.RegularPrice,
                    DefaultImage = pro.DefaultImage,
                    productcolorMaster = GetColorBestSellerSub(subcate_id, pro.Id)
                });
            }

            return list;
        }
        public int GetAllProductsBestSellerSub(int id)
        {
            //int i = 0;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT count(*) FROM Product p, Product_detail d, order_detail o " +
                    "where p.Id = d.Product_Id and d.Id = o.Product_detail_Id and p.subcategory_id =@id order by o.quantity desc";

                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);
                int count = Convert.ToInt32(cmd.ExecuteScalar());


                return count;
            }

        }
        //End BestSeller
        public List<ProductColorSub> GetProductsBySubcategoryID(int page, int id)
        {
            //int i = 0;
            List<ProductColorSub> list = new List<ProductColorSub>();
            foreach (var pro in GetProductsSub(page, id))
            {
                list.Add(new ProductColorSub()
                {
                    Id = pro.Id,
                    BrandId = pro.BrandId,
                    ProductName = pro.ProductName,
                    SalePrice = pro.SalePrice,
                    RegularPrice = pro.RegularPrice,
                    DefaultImage = pro.DefaultImage,
                    productcolorMaster = GetColorBySubId(id, pro.Id),
                    productvariant = GetVariantBySubId(pro.Id)
                });
            }

            return list;
        }
        public List<ProductColorSub> GetProductsBySizeSubID(int page, int size_id, int subcate_id)
        {
            //int i = 0;
            List<ProductColorSub> list = new List<ProductColorSub>();
            foreach (var pro in GetProductsSizeSub(page, size_id, subcate_id))
            {
                list.Add(new ProductColorSub()
                {
                    Id = pro.Id,
                    BrandId = pro.BrandId,
                    ProductName = pro.ProductName,
                    SalePrice = pro.SalePrice,
                    RegularPrice = pro.RegularPrice,
                    DefaultImage = pro.DefaultImage,
                    productcolorMaster = GetColorBySizeSub(subcate_id, pro.Id,size_id)
                });
            }

            return list;
        }
        public List<Color> GetColorRelateBaseSub(int product_id)

        {

            //int i = 0;
            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select color_id ,color_image from color c, product_color_variant p where p.color_id = c.id and p.product_id = @product_id order by color_id desc";

                MySqlCommand cmd = new MySqlCommand(str, conn);

                cmd.Parameters.AddWithValue("product_id", product_id);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = Convert.ToString(reader["color_image"]),

                        });
                    }
                }
            }
            return list;
        }
        public List<ProductColorSub> GetProductsRelateBaseSub(int product_id)
        {
            //int i = 0;
            List<ProductColorSub> list = new List<ProductColorSub>();
            foreach (var pro in GetProductsRelateSub(product_id))
            {
                list.Add(new ProductColorSub()
                {
                    Id = pro.Id,
                    BrandId = pro.BrandId,
                    ProductName = pro.ProductName,
                    SalePrice = pro.SalePrice,
                    RegularPrice = pro.RegularPrice,
                    DefaultImage = pro.DefaultImage,
                    productcolorMaster = GetColorRelateBaseSub(pro.Id),
                    productvariant = GetVariantBySubId(pro.Id)
                });
            }

            return list;
        }

        public List<Product> GetProductVariantByCate(int page, int id)

        {

            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT p.id as product_id, brand_id, ProductName,sale_price,regular_price, default_image FROM product p, subcategory s " +
                    "WHERE p.subcategory_id= s.id and s.category_id = @id order by p.id limit @page, 9";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);
                cmd.Parameters.AddWithValue("page", page);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["product_id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }
        public List<Color> GetColorByProId(int cate_id, int product_id)

        {

            //int i = 0;
            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct c.id as co_color_id, c.color_image as color_image from product_color_variant p , product a , subcategory s, color c " +
                    "where a.subcategory_id = s.id and s.category_id =@cate_id and c.id = p.color_id and p.product_id = @product_id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("product_id", product_id);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["co_color_id"]),
                            ColorImage = Convert.ToString(reader["color_image"]),

                        });
                    }
                }
            }
            return list;
        }


        public List<Product> GetProductsCate(int page, int cate_id)

        {

            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct  p.id as product_id , brand_id,ProductName,sale_price,regular_price, default_image " +
                    "from product p, subcategory s " +
                    "where p.subcategory_id = s.id and s.category_id=@cate_id limit @page, 9";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("page", page);


                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["product_id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }
        public List<Color> GetColorByCate(int cate_id, int product_id)

        {

            //int i = 0;
            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct color_id , color_image from color c , product p, subcategory s , product_detail d " +
                    "where c.id = d.color_id and p.id = d.product_id and p.subcategory_id = s.id and s.category_id = @cate_id and p.id =@product_id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("product_id", product_id);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = Convert.ToString(reader["color_image"]),

                        });
                    }
                }
            }
            return list;
        }
        //Start popular cate
        public List<Product> GetProPopularCate(int page, int cate_id)

        {

            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select p.id as product_id, brand_id, ProductName, sale_price, regular_price, default_image from Product p, subcategory c where p.active = 1 and p.subcategory_id= c.id and c.category_id=@cate_id limit @page, 9";


                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("page", page);


                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["product_id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }
        public List<Size> GetAllSizeSub(int subcate_id)

        {

            //int i = 0;
            List<Size> list = new List<Size>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct size_id, size_name from size s,product p , product_Detail d " +
                    "where p.id = d.product_id and p.subcategory_id = @subcate_id and s.id=d.size_id order by size_name";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);              
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Size()
                        {
                            Id = Convert.ToInt32(reader["size_id"]),
                            SizeName = reader["size_name"].ToString(),
                          
                        });
                    }
                }
            }
            return list;
        }
        public List<Size> GetAllSizeSubFilter(int subcate_id)

        {

            //int i = 0;
            List<Size> list = new List<Size>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct size_id, size_name from size s,product p , product_Detail d " +
                    "where p.id = d.product_id and p.subcategory_id = @subcate_id and s.id=d.size_id order by size_name";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Size()
                        {
                            Id = Convert.ToInt32(reader["size_id"]),
                            SizeName = reader["size_name"].ToString(),

                        });
                    }
                }
            }
            return list;
        }
        public List<Size> GetSizeCateID(int cate_id)

        {

            //int i = 0;
            List<Size> list = new List<Size>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct size_id, size_name from size s,product p , product_Detail d, subcategory a " +
                    "where a.id=p.subcategory_id and a.category_id = @cate_id and p.id = d.product_id and s.id=d.size_id order by size_name";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Size()
                        {
                            Id = Convert.ToInt32(reader["size_id"]),
                            SizeName = reader["size_name"].ToString(),

                        });
                    }
                }
            }
            return list;
        }
        public List<Color> GetAllColorSub(int subcate_id)
        {
            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct color_id , color_image from color c, product p , product_detail d " +
                    "where p.id = d.product_id and p.subcategory_id = @subcate_id and d.color_id = c.id order by color_id";
                   
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = reader["color_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }
        public List<Color> GetColorCateID(int cate_id)
        {
            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct color_id , color_image from color c, product p , product_detail d , subcategory a " +
                    "where p.id = d.product_id and p.subcategory_id = a.id and d.color_id = c.id and a.category_id =@cate_id order by color_id";

                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = reader["color_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }
        public List<Color> GetColorPopularCate(int cate_id, int product_id)

        {

            //int i = 0;
            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct color_id , color_image from color c , product p, subcategory s , product_detail d " +
                    "where c.id = d.color_id and p.id = d.product_id and p.subcategory_id = s.id and s.category_id = @cate_id and p.id =@product_id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("product_id", product_id);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = Convert.ToString(reader["color_image"]),

                        });
                    }
                }
            }
            return list;
        }
        public List<ProductColorSub> GetProductPopularCate(int page, int cate_id)
        {

            List<ProductColorSub> list = new List<ProductColorSub>();
            foreach (var pro in GetProPopularCate(page, cate_id))
            {
                list.Add(new ProductColorSub()
                {
                    Id = pro.Id,
                    BrandId = pro.BrandId,
                    ProductName = pro.ProductName,
                    SalePrice = pro.SalePrice,
                    RegularPrice = pro.RegularPrice,
                    DefaultImage = pro.DefaultImage,
                    productcolorMaster = GetColorPopularCate(cate_id, pro.Id)
                });
            }

            return list;
        }
        public int GetAllProductsPopularCate(int id)
        {
            //int i = 0;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT count(*) FROM product p, subcategory s WHERE p.subcategory_id= s.id and s.category_id = @id and p.active =1";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);
                int count = Convert.ToInt32(cmd.ExecuteScalar());


                return count;
            }

        }

        //End popular cate

        //start bestseller cate
        public List<Product> GetProBestSellerCate(int page, int cate_id)

        {

            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select p.id as product_id, brand_id, ProductName, sale_price, regular_price, default_image " +
                    "from Product p, Product_detail d, order_detail o, subcategory s" +
                     "  where p.Id = d.Product_Id and d.Id = o.Product_detail_Id and p.subcategory_id = s.id and" +
                     " s.category_id =@cate_id group by Productname order by count(o.Product_detail_Id) desc limit @page, 9";


                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("page", page);


                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["product_id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }
        public List<Color> GetColorBestSellerCate(int cate_id, int product_id)

        {

            //int i = 0;
            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct color_id , color_image from Product p, color c, Product_detail d, order_detail o, subcategory s" +
                            "  where p.id = @product_id and c.id=d.color_id and p.id = @product_id and p.Id = d.Product_Id and d.Id = o.Product_detail_Id and p.subcategory_id = s.id and " +
                            "s.category_id =@cate_id group by Productname order by count(o.Product_detail_Id) desc";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("product_id", product_id);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = Convert.ToString(reader["color_image"]),

                        });
                    }
                }
            }
            return list;
        }
        public List<ProductColorSub> GetProductBestSellerCate(int page, int cate_id)
        {

            List<ProductColorSub> list = new List<ProductColorSub>();
            foreach (var pro in GetProBestSellerCate(page, cate_id))
            {
                list.Add(new ProductColorSub()
                {
                    Id = pro.Id,
                    BrandId = pro.BrandId,
                    ProductName = pro.ProductName,
                    SalePrice = pro.SalePrice,
                    RegularPrice = pro.RegularPrice,
                    DefaultImage = pro.DefaultImage,
                    productcolorMaster = GetColorBestSellerCate(cate_id, pro.Id)
                });
            }

            return list;
        }
        public int GetAllProductsBestSellerCate(int id)
        {
            //int i = 0;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT count(*) FROM  Product p, Product_detail d, order_detail o, subcategory s " +
                            "where p.Id = d.Product_Id and d.Id = o.Product_detail_Id and p.subcategory_id = s.id and" +
                            " s.category_id =@id group by Productname order by count(o.Product_detail_Id) desc ";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);
                int count = Convert.ToInt32(cmd.ExecuteScalar());


                return count;
            }

        }
        //end bestseller cate

        //start New cate
        public List<Product> GetProNewCate(int page, int cate_id)

        {

            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select p.id as product_id, brand_id, ProductName, sale_price, regular_price, default_image from Product p, subcategory c where p.product_new =1  and p.subcategory_id= c.id and c.category_id=@cate_id limit @page, 9";


                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("page", page);


                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["product_id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }
        public List<Color> GetColorNewCate(int cate_id, int product_id)

        {

            //int i = 0;
            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct color_id , color_image from color c , product p, subcategory s , product_detail d " +
                    "where p.product_new = 1 and c.id = d.color_id and p.id = d.product_id and p.subcategory_id = s.id and s.category_id = @cate_id and p.id =@product_id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("product_id", product_id);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = Convert.ToString(reader["color_image"]),

                        });
                    }
                }
            }
            return list;
        }
        public List<ProductColorSub> GetProductNewCate(int page, int cate_id)
        {

            List<ProductColorSub> list = new List<ProductColorSub>();
            foreach (var pro in GetProNewCate(page, cate_id))
            {
                list.Add(new ProductColorSub()
                {
                    Id = pro.Id,
                    BrandId = pro.BrandId,
                    ProductName = pro.ProductName,
                    SalePrice = pro.SalePrice,
                    RegularPrice = pro.RegularPrice,
                    DefaultImage = pro.DefaultImage,
                    productcolorMaster = GetColorNewCate(cate_id, pro.Id)
                });
            }

            return list;
        }
        public int GetAllProductsNewCate(int id)
        {
            //int i = 0;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT count(*) FROM product p, subcategory s WHERE p.subcategory_id= s.id and s.category_id = @id and p.product_new =1";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);
                int count = Convert.ToInt32(cmd.ExecuteScalar());


                return count;
            }

        }
        //end New cate

        //start ASC cate
        public List<Product> GetProASCCate(int page, int cate_id)

        {

            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select p.id as product_id, brand_id, ProductName, sale_price, regular_price, default_image" +
                    " from Product p, subcategory s where s.id= p.subcategory_id and s.category_id = @cate_id  order by sale_price asc limit @page, 9";



                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("page", page);


                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["product_id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }
        public List<Color> GetColorASCCate(int cate_id, int product_id)

        {

            //int i = 0;
            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct color_id , color_image from color c , product p, subcategory s , product_detail d " +
                    "where c.id = d.color_id and p.id = d.product_id and  p.id = @product_id and s.id= p.subcategory_id and s.category_id = @cate_id  order by sale_price asc";

                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("product_id", product_id);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = Convert.ToString(reader["color_image"]),

                        });
                    }
                }
            }
            return list;
        }
        public List<ProductColorSub> GetProductASCCate(int page, int cate_id)
        {

            List<ProductColorSub> list = new List<ProductColorSub>();
            foreach (var pro in GetProASCCate(page, cate_id))
            {
                list.Add(new ProductColorSub()
                {
                    Id = pro.Id,
                    BrandId = pro.BrandId,
                    ProductName = pro.ProductName,
                    SalePrice = pro.SalePrice,
                    RegularPrice = pro.RegularPrice,
                    DefaultImage = pro.DefaultImage,
                    productcolorMaster = GetColorASCCate(cate_id, pro.Id)
                });
            }

            return list;
        }
        public int GetAllProductsASCCate(int id)
        {
            //int i = 0;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT count(*) from Product p, subcategory s where s.id= p.subcategory_id and s.category_id = @id  order by sale_price asc";

                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);
                int count = Convert.ToInt32(cmd.ExecuteScalar());


                return count;
            }

        }
        //end ASC cate

        //start DESC cate
        public List<Product> GetProDESCCate(int page, int cate_id)

        {

            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select p.id as product_id, brand_id, ProductName, sale_price, regular_price, default_image from Product p, subcategory s where s.id= p.subcategory_id and s.category_id = @cate_id order by sale_price desc limit @page, 9";


                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("page", page);


                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["product_id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }
        public List<Color> GetColorDESCCate(int cate_id, int product_id)

        {

            //int i = 0;
            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct color_id , color_image from color c , product p, subcategory s , product_detail d " +
                    "where c.id = d.color_id and p.id=@product_id and s.id= p.subcategory_id and s.category_id = @cate_id and p.id = d.product_id order by sale_price desc";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("product_id", product_id);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = Convert.ToString(reader["color_image"]),

                        });
                    }
                }
            }
            return list;
        }
        public List<ProductColorSub> GetProductDESCCate(int page, int cate_id)
        {

            List<ProductColorSub> list = new List<ProductColorSub>();
            foreach (var pro in GetProDESCCate(page, cate_id))
            {
                list.Add(new ProductColorSub()
                {
                    Id = pro.Id,
                    BrandId = pro.BrandId,
                    ProductName = pro.ProductName,
                    SalePrice = pro.SalePrice,
                    RegularPrice = pro.RegularPrice,
                    DefaultImage = pro.DefaultImage,
                    productcolorMaster = GetColorDESCCate(cate_id, pro.Id)
                });
            }

            return list;
        }
        public int GetAllProductDESCCate(int id)
        {
            //int i = 0;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT count(*) from Product p, subcategory s where s.id= p.subcategory_id and s.category_id = @id order by sale_price desc";

                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);
                int count = Convert.ToInt32(cmd.ExecuteScalar());


                return count;
            }

        }
        //end DESC cate

        //start search 
        public List<Product> GetProSearchCate(int page, string keyword)

        {

            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select * from Product where Productname LIKE CONCAT('%', @keyword, '%') limit @page, 9";


                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("keyword", keyword);
                cmd.Parameters.AddWithValue("page", page);


                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }
        public List<Color> GetColorSearch(int id)

        {

            //int i = 0;
            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct color_id , color_image  from color c , product p, product_detail d" +
                    " where c.id = d.color_id and p.id = d.product_id and p.id =@id order by product_id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);


                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = Convert.ToString(reader["color_image"]),

                        });
                    }
                }
            }
            return list;
        }
        public List<ProductColorSub> GetProductsSearch(int page, string keyword)
        {

            List<ProductColorSub> list = new List<ProductColorSub>();
            foreach (var pro in GetProSearchCate(page, keyword))
            {
                list.Add(new ProductColorSub()
                {
                    Id = pro.Id,
                    BrandId = pro.BrandId,
                    ProductName = pro.ProductName,
                    SalePrice = pro.SalePrice,
                    RegularPrice = pro.RegularPrice,
                    DefaultImage = pro.DefaultImage,
                    productcolorMaster = GetColorSearch(pro.Id)
                });
            }

            return list;
        }
        public int GetAllProductsSearch(string keyword)
        {
            //int i = 0;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT count(*) FROM Product where Productname LIKE CONCAT('%', @keyword, '%')";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("keyword", keyword);
                int count = Convert.ToInt32(cmd.ExecuteScalar());


                return count;
            }

        }

        //end search
        public List<ProductColorSub> GetProductsCateObj(int page, int cate_id)
        {
            //int i = 0;
            List<ProductColorSub> list = new List<ProductColorSub>();
            foreach (var pro in GetProductsCate(page, cate_id))
            {
                list.Add(new ProductColorSub()
                {
                    Id = pro.Id,
                    BrandId = pro.BrandId,
                    ProductName = pro.ProductName,
                    SalePrice = pro.SalePrice,
                    RegularPrice = pro.RegularPrice,
                    DefaultImage = pro.DefaultImage,
                    productcolorMaster = GetColorByCate(cate_id, pro.Id),
                    productvariant = GetVariantBySubId(pro.Id)
                });
            }

            return list;
        }


        public int GetAllProductsCate(int id)
        {
            //int i = 0;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT count(*) FROM product p, subcategory s WHERE p.subcategory_id= s.id and s.category_id = @id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);
                int count = Convert.ToInt32(cmd.ExecuteScalar());


                return count;
            }

        }

        public int GetAllProductsPopularSub(int id)
        {
            //int i = 0;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT count(*) FROM product  WHERE subcategory_id= @id and active =1";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);
                int count = Convert.ToInt32(cmd.ExecuteScalar());


                return count;
            }

        }
        public int GetAllProductsSub(int id)
        {
            //int i = 0;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT count(*) FROM product  WHERE subcategory_id= @id ";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);
                int count = Convert.ToInt32(cmd.ExecuteScalar());


                return count;
            }

        }

        
             public int GetAllProductSizeSub(int size_id, int subcate_id)
              {
            //int i = 0;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT count(distinct p.id) from product p , product_detail d " +
                    "where p.subcategory_id = @subcate_id and p.id= d.product_id and d.size_id = @size_id ";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);
                cmd.Parameters.AddWithValue("size_id", size_id);
                int count = Convert.ToInt32(cmd.ExecuteScalar());


                return count;
            }

        }
        public List<ProductColorSub> GetProductsByColorSubID(int page, int color_id, int subcate_id)
        {
            //int i = 0;
            List<ProductColorSub> list = new List<ProductColorSub>();
            foreach (var pro in GetProductsColorSub(page, color_id, subcate_id))
            {
                list.Add(new ProductColorSub()
                {
                    Id = pro.Id,
                    BrandId = pro.BrandId,
                    ProductName = pro.ProductName,
                    SalePrice = pro.SalePrice,
                    RegularPrice = pro.RegularPrice,
                    DefaultImage = pro.DefaultImage,
                    productcolorMaster = GetColorByColorSub(subcate_id, pro.Id)
                });
            }

            return list;
        }



        public List<Product> GetProductsColorSub(int page, int color_id, int subcate_id)

        {

            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT distinct p.id as product_id, brand_id, ProductName,sale_price,regular_price, default_image from product p , product_detail d " +
                  "where p.subcategory_id = @subcate_id and p.id= d.product_id and d.color_id = @color_id limit @page, 9";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("color_id", color_id);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);
                cmd.Parameters.AddWithValue("page", page);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["product_id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }

        public List<ProductColorSub> GetProductsByColorCateID(int page, int color_id, int cate_id)
        {
            //int i = 0;
            List<ProductColorSub> list = new List<ProductColorSub>();
            foreach (var pro in GetProductsColorCate(page, color_id, cate_id))
            {
                list.Add(new ProductColorSub()
                {
                    Id = pro.Id,
                    BrandId = pro.BrandId,
                    ProductName = pro.ProductName,
                    SalePrice = pro.SalePrice,
                    RegularPrice = pro.RegularPrice,
                    DefaultImage = pro.DefaultImage,
                    productcolorMaster = GetColorByColorCate(cate_id, pro.Id)
                });
            }

            return list;
        }



        public List<Product> GetProductsColorCate(int page, int color_id, int cate_id)

        {

            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT distinct p.id as product_id, brand_id, ProductName,sale_price,regular_price, default_image from product p , product_detail d , subcategory s " +
                 "where p.subcategory_id = s.id and p.id= d.product_id and d.color_id = @color_id and s.category_id = @cate_id limit @page ,9";
                MySqlCommand cmd = new MySqlCommand(str, conn);

                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("color_id", color_id);
                cmd.Parameters.AddWithValue("page", page);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["product_id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }


        public List<ProductColorSub> GetProductsByColorSizeCateID(int page, int color_id, int size_id, int cate_id)
        {
            //int i = 0;
            List<ProductColorSub> list = new List<ProductColorSub>();
            foreach (var pro in GetProductsColorSizeCate(page, color_id, size_id, cate_id))
            {
                list.Add(new ProductColorSub()
                {
                    Id = pro.Id,
                    BrandId = pro.BrandId,
                    ProductName = pro.ProductName,
                    SalePrice = pro.SalePrice,
                    RegularPrice = pro.RegularPrice,
                    DefaultImage = pro.DefaultImage,
                    productcolorMaster = GetColorByColorSizeCate(cate_id, pro.Id)
                });
            }

            return list;
        }



        public List<Product> GetProductsColorSizeCate(int page, int color_id, int size_id, int cate_id)

        {

            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT distinct p.id as product_id, brand_id, ProductName,sale_price,regular_price, default_image from product p , product_detail d , subcategory s " +
                 "where p.subcategory_id = s.id and p.id= d.product_id and d.color_id = @color_id and s.category_id = @cate_id and d.size_id = @size_id limit @page ,9";
                MySqlCommand cmd = new MySqlCommand(str, conn);

                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("color_id", color_id);
                cmd.Parameters.AddWithValue("size_id", size_id);
                cmd.Parameters.AddWithValue("page", page);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["product_id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }






        public List<Color> GetColorByColorSizeCate(int cate_id, int product_id)

        {

            //int i = 0;
            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct color_id, color_image from product p , product_detail d, color c , subcategory s " +
                "where p.id = d.product_id and d.color_id = c.id and p.subcategory_id = s.id and s.category_id = @cate_id and p.id = @product_id ";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("product_id", product_id);


                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = Convert.ToString(reader["color_image"]),

                        });
                    }
                }
            }
            return list;
        }



        public int GetAllProductColorSizeCate(int color_id, int size_id, int cate_id)
        {
            //int i = 0;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT count(distinct p.id) from product p , product_detail d, subcategory s " +
                 "where p.subcategory_id = s.id and s.category_id = @cate_id and p.id= d.product_id and d.color_id = @color_id and d.size_id = @size_id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("color_id", color_id);
                cmd.Parameters.AddWithValue("size_id", size_id);
                int count = Convert.ToInt32(cmd.ExecuteScalar());


                return count;
            }

        }




        public List<Color> GetColorByColorCate(int cate_id, int product_id)

        {

            //int i = 0;
            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct color_id, color_image from product p , product_detail d, color c , subcategory s " +
                "where p.id = d.product_id and d.color_id = c.id and p.subcategory_id = s.id and s.category_id = @cate_id and p.id = @product_id ";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("product_id", product_id);


                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = Convert.ToString(reader["color_image"]),

                        });
                    }
                }
            }
            return list;
        }



        public int GetAllProductColorCate(int color_id, int cate_id)
        {
            //int i = 0;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT count(distinct p.id) from product p , product_detail d, subcategory s " +
                 "where p.subcategory_id = s.id and s.category_id = @cate_id and p.id= d.product_id and d.color_id = @color_id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("color_id", color_id);
                int count = Convert.ToInt32(cmd.ExecuteScalar());


                return count;
            }

        }





        public List<Color> GetColorByColorSub(int subcate_id, int product_id)

        {

            //int i = 0;
            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct color_id, color_image from product p , product_detail d, color c " +
                     "where p.id = d.product_id and d.color_id = c.id and p.subcategory_id = @subcate_id and p.id = @product_id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);
                cmd.Parameters.AddWithValue("product_id", product_id);


                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = Convert.ToString(reader["color_image"]),

                        });
                    }
                }
            }
            return list;
        }



        public int GetAllProductColorSub(int color_id, int subcate_id)
        {
            //int i = 0;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT count(distinct p.id) from product p , product_detail d " +
                    "where p.subcategory_id = @subcate_id and p.id= d.product_id and d.color_id = @color_id ";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);
                cmd.Parameters.AddWithValue("color_id", color_id);
                int count = Convert.ToInt32(cmd.ExecuteScalar());


                return count;
            }

        }

        public List<ProductColorSub> GetProductsByColorSizeSubID(int page, int color_id, int size_id, int subcate_id)
        {
            //int i = 0;
            List<ProductColorSub> list = new List<ProductColorSub>();
            foreach (var pro in GetProductsColorSizeSub(page, color_id, size_id, subcate_id))
            {
                list.Add(new ProductColorSub()
                {
                    Id = pro.Id,
                    BrandId = pro.BrandId,
                    ProductName = pro.ProductName,
                    SalePrice = pro.SalePrice,
                    RegularPrice = pro.RegularPrice,
                    DefaultImage = pro.DefaultImage,
                    productcolorMaster = GetColorByColorSizeSub(subcate_id, pro.Id, size_id)
                });
            }

            return list;
        }



        public List<Product> GetProductsColorSizeSub(int page, int color_id, int size_id, int subcate_id)

        {

            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT distinct p.id as product_id, brand_id, ProductName,sale_price,regular_price, default_image from product p , product_detail d " +
                  "where p.subcategory_id = @subcate_id and p.id= d.product_id and d.color_id = @color_id and d.size_id =@size_id limit @page, 9 ";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("color_id", color_id);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);
                cmd.Parameters.AddWithValue("size_id", size_id);
                cmd.Parameters.AddWithValue("page", page);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["product_id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }






        public List<Color> GetColorByColorSizeSub(int subcate_id, int product_id, int size_id)

        {

            //int i = 0;
            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct color_id, color_image from product p , product_detail d, color c " +
               " where p.id = d.product_id and d.color_id = c.id and p.subcategory_id = @subcate_id and p.id = @product_id ";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);
                cmd.Parameters.AddWithValue("product_id", product_id);


                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = Convert.ToString(reader["color_image"]),

                        });
                    }
                }
            }
            return list;
        }



        public int GetAllProductColorSizeSub(int color_id, int size_id , int subcate_id)
        {
            //int i = 0;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT count(distinct p.id) from product p , product_detail d " +
                    "where p.subcategory_id = @subcate_id and p.id= d.product_id and d.color_id = @color_id and d.size_id = @size_id ";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("subcate_id", subcate_id);
                cmd.Parameters.AddWithValue("color_id", color_id);
                cmd.Parameters.AddWithValue("size_id", size_id);
                int count = Convert.ToInt32(cmd.ExecuteScalar());


                return count;
            }

        }

        public List<ProductColorSub> GetProductsBySizeCateID(int page, int size_id, int cate_id)
        {
            //int i = 0;
            List<ProductColorSub> list = new List<ProductColorSub>();
            foreach (var pro in GetProductsSizeCate(page, size_id, cate_id))
            {
                list.Add(new ProductColorSub()
                {
                    Id = pro.Id,
                    BrandId = pro.BrandId,
                    ProductName = pro.ProductName,
                    SalePrice = pro.SalePrice,
                    RegularPrice = pro.RegularPrice,
                    DefaultImage = pro.DefaultImage,
                    productcolorMaster = GetColorBySizeCate(cate_id, pro.Id)
                });
            }

            return list;
        }



        public List<Product> GetProductsSizeCate(int page, int size_id, int cate_id)

        {

            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT distinct p.id as product_id, brand_id, ProductName,sale_price,regular_price, default_image from product p , product_detail d , subcategory s " +
               "where p.subcategory_id = s.id and p.id= d.product_id and d.size_id = @size_id and s.category_id = @cate_id limit @page, 9";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("size_id", size_id);
                cmd.Parameters.AddWithValue("page", page);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["product_id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            RegularPrice = Convert.ToInt64(reader["regular_price"]),
                            DefaultImage = reader["default_image"].ToString(),

                        });
                    }
                }
            }
            return list;
        }






        public List<Color> GetColorBySizeCate(int cate_id, int product_id)

        {

            //int i = 0;
            List<Color> list = new List<Color>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct color_id, color_image from product p , product_detail d, color c , subcategory s "+
                "where p.id = d.product_id and d.color_id = c.id and p.subcategory_id = s.id and s.category_id = @cate_id and p.id = @product_id ";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("product_id", product_id);


                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Color()
                        {
                            Id = Convert.ToInt32(reader["color_id"]),
                            ColorImage = Convert.ToString(reader["color_image"]),

                        });
                    }
                }
            }
            return list;
        }



        public int GetAllProductSizeCate(int size_id, int cate_id)
        {
            //int i = 0;

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT count(distinct p.id) from product p , product_detail d, subcategory s " +
                 "where p.subcategory_id = s.id and s.category_id = @cate_id and p.id= d.product_id and d.size_id =@size_id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("cate_id", cate_id);
                cmd.Parameters.AddWithValue("size_id", size_id);
                int count = Convert.ToInt32(cmd.ExecuteScalar());


                return count;
            }

        }













        public List<Product> GetProductWoman()
        {
            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select * from Product where gender = 0";
                MySqlCommand cmd = new MySqlCommand(str, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            BrandId = Convert.ToInt32(reader["brand_id"]),
                            ProductName = reader["ProductName"].ToString(),
                            SalePrice = Convert.ToInt64(reader["sale_price"]),
                            DefaultImage = reader["default_image"].ToString(),


                        });
                    }
                }
            }
            return list;
        }




        public List<Brand> GetBrand()
        {
            //int i = 0;
            List<Brand> list = new List<Brand>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select *from Brand";
                MySqlCommand cmd = new MySqlCommand(str, conn);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Brand()
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            BrandName = reader["Brand_name"].ToString(),
                            Active = Convert.ToBoolean(reader["active"])
                        });
                    }
                }
            }
            return list;
        }

        public List<Product> GetProductBrand(int IdBrand)
        {
            //int i = 0;
            List<Product> list = new List<Product>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct p.Id, ProductName from Product p,Product_detail a, Brand b where p.Id= a.Product_Id and " +
                    " p.Brand_Id=@IdBrand";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("IdBrand", IdBrand);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            ProductName = reader["ProductName"].ToString(),


                        });
                    }
                }
            }
            return list;
        }
        public List<Size> GetSizeByIDPro(int color_id, int product_id)
        {
            //int i = 0;
            List<Size> list = new List<Size>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select distinct size_name , s.id as size_id from size s, product_detail d" +
                    " where s.id = d.size_id and d.product_id=@product_id and d.color_id=@color_id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("product_id", product_id);
                cmd.Parameters.AddWithValue("color_id", color_id);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Size()
                        {

                            SizeName = reader["size_name"].ToString(),
                            Id = Convert.ToInt32(reader["size_id"])


                        });
                    }
                }
            }
            return list;
        }
     /*   public ProductObject GetProductVariantDetail(int color_id, int product_id)
        {
            ProductObject category = new ProductObject();
            using (var conn = GetConnection())
            {
                conn.Open();
                string str = "SELECT * FROM category c,subcategory s, product p, product_detail d,brand e, color a, size b " +
        "where d.size_id = b.id and d.color_id = a.id and p.id=d.product_id and e.id=p.brand_id and s.id = p.subcategory_id and c.id = s.category_id and p.id =@id";

                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("color_id", color_id);
                cmd.Parameters.AddWithValue("product_id", product_id);
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    category.ProductId = Convert.ToInt32(reader["product_id"]);
                    category.ColorId = Convert.ToInt32(reader["color_id"]);
                    category.BrandId = Convert.ToInt32(reader["brand_id"]);
                    category.ProductName = reader["productName"].ToString();
                    category.BrandName = reader["brand_name"].ToString();
                    category.ProductTag = reader["product_tag"].ToString();
                    category.SalePrice = Convert.ToInt64(reader["sale_price"]);
                    category.RegularPrice = Convert.ToInt64(reader["regular_price"]);
                    category.ColorName = reader["color_name"].ToString();
                    category.ColorImage = reader["color_image"].ToString();
                    category.SizeName = reader["Size_name"].ToString();
                    category.Quantity = Convert.ToInt32(reader["quantity"]);
                    category.Category_id = Convert.ToInt32(reader["category_id"]);
                    category.Subcategory_id = Convert.ToInt32(reader["subcategory_id"]);
                    category.CategoryName = reader["categoryname"].ToString();
                    category.SubcategoryName = reader["subcategory_name"].ToString();



                    category.Product_detail_img_1 = reader["product_detail_img_1"].ToString();
                    category.Product_detail_img_2 = reader["product_detail_img_2"].ToString();
                    category.Product_detail_img_3 = reader["product_detail_img_3"].ToString();
                    category.Product_detail_img_4 = reader["product_detail_img_4"].ToString();
                    category.Product_detail_img_5 = reader["product_detail_img_5"].ToString();
                    category.Product_detail_img_6 = reader["product_detail_img_6"].ToString();
                    category.Product_detail_big_img_1 = reader["product_detail_big_img_1"].ToString();
                    category.Product_detail_big_img_2 = reader["product_detail_big_img_2"].ToString();
                    category.Product_detail_big_img_3 = reader["product_detail_big_img_3"].ToString();
                    category.Product_detail_big_img_4 = reader["product_detail_big_img_4"].ToString();
                    category.Product_detail_big_img_5 = reader["product_detail_big_img_5"].ToString();
                    category.Product_detail_big_img_6 = reader["product_detail_big_img_6"].ToString();
                }
            }
            return category;
        } */

        public ProductObject GetProductObject(int color_id, int product_id)
        {
            ProductObject category = new ProductObject();
            using (var conn = GetConnection())
            {
                conn.Open();
                string str = "SELECT * FROM category c,subcategory s, product p, product_detail d,brand e, color a, size b, product_color_variant v " +
                    "where d.size_id = b.id and d.color_id = a.id and d.product_id=@product_id and e.id = p.brand_id and s.id = p.subcategory_id " +
                    "and c.id = s.category_id and p.id=d.product_id and  a.id=@color_id and v.product_id = p.id and v.color_id = d.color_id";

                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("color_id", color_id);
                cmd.Parameters.AddWithValue("product_id", product_id);
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    category.ProductId = Convert.ToInt32(reader["product_id"]);
                    category.ColorId = Convert.ToInt32(reader["color_id"]);
                    category.BrandId = Convert.ToInt32(reader["brand_id"]);
                    category.ProductName = reader["productName"].ToString();
                    category.BrandName = reader["brand_name"].ToString();
                    category.ProductTag = reader["product_tag"].ToString();
                    category.SalePrice = Convert.ToInt64(reader["sale_price"]);
                    category.RegularPrice = Convert.ToInt64(reader["regular_price"]);
                    category.ColorName = reader["color_name"].ToString();
                    category.ColorImage = reader["color_image"].ToString();
                    category.SizeName = reader["Size_name"].ToString();
                    category.Quantity = Convert.ToInt32(reader["quantity"]);
                    category.Category_id = Convert.ToInt32(reader["category_id"]);
                    category.Subcategory_id = Convert.ToInt32(reader["subcategory_id"]);
                    category.CategoryName = reader["categoryname"].ToString();
                    category.SubcategoryName = reader["subcategory_name"].ToString();
                    category.ImgBig1 = reader["img_big_1"].ToString();
                    category.ImgBig2 = reader["img_big_2"].ToString();
                    category.ImgBig3 = reader["img_big_3"].ToString();
                    category.ImgBig4 = reader["img_big_4"].ToString();
                    category.ImgBig5 = reader["img_big_5"].ToString();
                    category.ImgBig6 = reader["img_big_6"].ToString();
                    
                    category.Gender = Convert.ToInt32(reader["gender"]);
                }
            }
            return category;
        }

        public SubCateObject GetSubCate(int id)
        {
            SubCateObject category = new SubCateObject();
            using (var conn = GetConnection())
            {
                conn.Open();
                string str = "SELECT categoryName, category_id, subcategory_name, s.id as subcategory_id from category c, subcategory s where c.id = s.category_id and s.id=@id";

                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("id", id);
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    category.Category_name = reader["categoryName"].ToString();
                    category.Category_id = Convert.ToInt32(reader["category_id"]);
                    category.SubCate_id = Convert.ToInt32(reader["subcategory_id"]);
                    category.SubCategory_name = reader["subcategory_name"].ToString();

                }
            }
            return category;
        }
        public List<ProductQuantityBySize> GetSizeID(int color_id, int size_id, int product_id)
        {
            //int i = 0;
            List<ProductQuantityBySize> list = new List<ProductQuantityBySize>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select quantity, size_name from size s, product_detail d where d.product_id = @product_id and d.color_id = @color_id and d.size_id = s.id and s.id =@size_id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("color_id", color_id);
                cmd.Parameters.AddWithValue("product_id", product_id);
                cmd.Parameters.AddWithValue("size_id", size_id);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new ProductQuantityBySize()
                        {

                            SizeName = reader["size_name"].ToString(),
                            Quantity = Convert.ToInt32(reader["quantity"])


                        });
                    }
                }
            }
            return list;
        }
        public List<ProductVariantDefault> GetVariantImg(int product_id, int color_id)
        {
            //int i = 0;
            List<ProductVariantDefault> list = new List<ProductVariantDefault>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "select product_variant_image, default_image from product p, product_color_variant d where p.id =d.product_id and d.product_id= @product_id and color_id = @color_id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("product_id", product_id);
                cmd.Parameters.AddWithValue("color_id", color_id);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new ProductVariantDefault()
                        {

                            ProductVariantImage = reader["product_variant_image"].ToString(),
                            ProductDefault = reader["default_image"].ToString()



                        });
                    }
                }
            }
            return list;
        }

        public List<Comment> GetComment(int product_id)
        {
            //int i = 0;
            List<Comment> list = new List<Comment>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT * FROM comment where comment_product_id = @product_id and comment_status = 1 order by comment_date desc";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("product_id", product_id);
             
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Comment()
                        {
                            ID = Convert.ToInt32(reader["comment_id"]),
                            CommentName = Convert.ToString(reader["comment_name"]),
                            CommentText = Convert.ToString(reader["comment_text"]),
                            CommentProductID =Convert.ToInt32(reader["comment_product_id"]),
                            CommentDate = Convert.ToDateTime(reader["comment_date"]),
                        }); 
                    }
                }
            }
            return list;
        }
        public List<Comment> ShowCommentParent(int comment_id)
        {
            //int i = 0;
            List<Comment> list = new List<Comment>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT * FROM comment where comment_parent_comment =@comment_id order by comment_date desc";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("comment_id", comment_id);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Comment()
                        {
                            ID = Convert.ToInt32(reader["comment_id"]),
                            CommentName = Convert.ToString(reader["comment_name"]),
                            CommentText = Convert.ToString(reader["comment_text"]),
                            CommentProductID = Convert.ToInt32(reader["comment_product_id"]),
                            CommentDate = Convert.ToDateTime(reader["comment_date"]),
                            CommentParentComment = Convert.ToInt32(reader["comment_parent_comment"]),
                        });
                    }
                }
            }
            return list;
        }
        public List<Object> GetCommentAdmin()
        {
            //int i = 0;
            List<Object> list = new List<Object>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "SELECT distinct (comment_id) , comment_text,comment_name,comment_status, comment_date , color_id, product_id, productName " +
                    "from product_detail p , comment c , product d " +
                    "where p.color_id = c.comment_color_id and d.id= p.product_id and c.comment_product_id = p.product_id order by comment_date desc";


                MySqlCommand cmd = new MySqlCommand(str, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {


                        var ob = new 
                        {
                            comment_name = reader["comment_name"].ToString(),
                            product_name = reader["productName"].ToString(),
                            comment_text = reader["comment_text"].ToString(),
                            comment_id = Convert.ToInt32(reader["comment_id"]),
                            color_id = Convert.ToInt32(reader["color_id"]),
                            comment_status = Convert.ToInt32(reader["comment_status"]),
                            // color_id = Convert.ToInt32(reader["p.color_id"]),
                            product_id = Convert.ToInt32(reader["product_id"]),
                            comment_date = Convert.ToDateTime(reader["comment_date"]),
                                                     

                        };
                        list.Add(ob);
                    }
                   reader.Close();
                }
                conn.Close();
            }
            return list;
        }

        public int  InsertComment( string comment_name,int product_id ,string comment_text, int comment_status, int comment_color_id)
        {
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "insert into Comment values(null,@comment_name,null,@product_id,@comment_text,@comment_status,@comment_color_id,null)";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("comment_name", comment_name);
                cmd.Parameters.AddWithValue("product_id", product_id);
                cmd.Parameters.AddWithValue("comment_text", comment_text);
                cmd.Parameters.AddWithValue("comment_status", comment_status);
                cmd.Parameters.AddWithValue("comment_color_id", comment_color_id);
                return (cmd.ExecuteNonQuery());

            }
        }
        public int ReplyComment(string comment_name, int product_id, string comment_text, int comment_status, int comment_color_id, int comment_parent_comment)
        {
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "insert into Comment values(null,@comment_name,null,@product_id,@comment_text,@comment_status,@comment_color_id,@comment_parent_comment)";
                MySqlCommand cmd = new MySqlCommand(str, conn);
              cmd.Parameters.AddWithValue("comment_name", comment_name);
                cmd.Parameters.AddWithValue("product_id", product_id);
                cmd.Parameters.AddWithValue("comment_text", comment_text);
                cmd.Parameters.AddWithValue("comment_status", comment_status);
                cmd.Parameters.AddWithValue("comment_color_id", comment_color_id);
                cmd.Parameters.AddWithValue("comment_parent_comment", comment_parent_comment);
                return (cmd.ExecuteNonQuery());

            }
        }

        public int UpdateComment(int product_id, string comment_id, int comment_status,int color_id)
        {
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                var str = "update comment" +
                    " set comment_status = @comment_status" +
                    " where comment_product_id = @product_id and comment_id = @comment_id and comment_color_id = @color_id";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("comment_id", comment_id);
                cmd.Parameters.AddWithValue("product_id", product_id);
                cmd.Parameters.AddWithValue("comment_status", comment_status);
                cmd.Parameters.AddWithValue("color_id", color_id);
          
                return (cmd.ExecuteNonQuery());

            }
        }


        //CartItem CRUD - start
        public int InsertCartItem(CartItem item)
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                string str = "insert into cart_item (user_id, product_detail_id, quantity) values (@uid, @pid, @quan)";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("uid", item.UserId);
                cmd.Parameters.AddWithValue("pid", item.ProductDetailId);
                cmd.Parameters.AddWithValue("quan", item.Quantity);
                return cmd.ExecuteNonQuery();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }
        public int DeleteCartItem(CartItem item)
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                string str = "delete from cart_item where user_id = @uid and product_detail_id = @pid";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("uid", item.UserId);
                cmd.Parameters.AddWithValue("pid", item.ProductDetailId);
                return cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }
        public int AddToCart(CartItem item)
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                string str = "select * from cart_item where user_id = @uid and product_detail_id = @pid";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("uid", item.UserId);
                cmd.Parameters.AddWithValue("pid", item.ProductDetailId);
                using var reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    return UpdateAddCartItem(item);
                }
                else return InsertCartItem(item);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }
        public int DeleteAllUserCart(int UID)
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                string str = "delete from cart_item where user_id = @uid ";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("uid", UID);
                return cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }
        public int UpdateAddCartItem(CartItem item)
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                string str = "update cart_item set quantity = quantity + @quan where user_id = @uid and product_detail_id = @pid";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("uid", item.UserId);
                cmd.Parameters.AddWithValue("pid", item.ProductDetailId);
                cmd.Parameters.AddWithValue("quan", item.Quantity);
                return cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }
        public List<CartItem> GetCartItemList(int UID)
        {
            List<CartItem> resultList = new List<CartItem>();
            try
            {
                using var conn = GetConnection();
                conn.Open();
                string str = "select * from cart_item where user_id = @uid";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("uid", UID);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    CartItem item = new CartItem
                    {
                        UserId = UID,
                        ProductDetailId = Convert.ToInt32(reader["product_detail_id"]),
                        Quantity = Convert.ToInt32(reader["quantity"])
                    };
                    resultList.Add(item);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return resultList;
        }
        public void UpdateCartList(List<CartItemDetail> sessionCartList, int UID)
        {
            DeleteAllUserCart(UID);
            foreach(CartItem item in sessionCartList)
            {
                item.UserId = UID;
                InsertCartItem(item);
            }    
        }

        //CartItem CRUD - end

        //Order CRUD - start
        public int CreateOrder(Order order, List<CartItem> itemList)
        {
            try
            {
                string insertOrderSql = "insert into `order` (uid, order_date, address, name, phone, total) values (@uid, @date, @address, @name, @phone, @total)";
                string getOrderId = "select last_insert_id()";
                string insertOrderDetailSql = "insert into order_detail (order_id, product_detail_id, quantity) values (@orderId, @pid, @quantity)";
                using var conn = GetConnection();
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(insertOrderSql, conn);
                cmd.Parameters.AddWithValue("uid", order.UID);
                cmd.Parameters.AddWithValue("date", order.OrderDate);
                cmd.Parameters.AddWithValue("address", order.Address);
                cmd.Parameters.AddWithValue("name", order.Name);
                cmd.Parameters.AddWithValue("phone", order.Phone);
                order.Total = 0;
                foreach(var item in itemList)
                {
                    CartItemDetail detail = item.ParseCartDetailItem(this);
                    order.Total += detail.PricePerUnit * detail.Quantity;
                }
                cmd.Parameters.AddWithValue("total", order.Total);
                cmd.ExecuteNonQuery();

                cmd = new MySqlCommand(getOrderId, conn);
                order.ID = Convert.ToInt32(cmd.ExecuteScalar());

                foreach(var item in itemList)
                {
                    cmd = new MySqlCommand(insertOrderDetailSql, conn);
                    cmd.Parameters.AddWithValue("orderId", order.ID);
                    cmd.Parameters.AddWithValue("pid", item.ProductDetailId);
                    cmd.Parameters.AddWithValue("quantity", item.Quantity);
                    cmd.ExecuteNonQuery();
                }

                return 1;
            }
            catch(Exception e)
            {
                return -1;
            }
        }
        //Order CRUD - start
        public List<AdminModel> GetAllAdminUser()
        {
            List<AdminModel> resultList = new List<AdminModel>();
            try
            {
                using var conn = GetConnection();
                conn.Open();
                string str = "select * from admin";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    AdminModel item = new AdminModel
                    {
                        Username = reader["username"].ToString(),
                        Password = reader["password"].ToString(),
                    };
                    resultList.Add(item);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return resultList;
        }
        //Analysis
        public List<object> SoLuongGiay()
        {
            List<object> list = new List<object>();

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();

                string str = "select p.productname,pt.quantity as SL from product_detail pt,PRODUCT p where p.id = pt.product_id group by p.id";

                MySqlCommand cmd = new MySqlCommand(str, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var ob = new { tengiay = reader["productname"].ToString(), soluong = Convert.ToInt32(reader["SL"]) };
                        list.Add(ob);

                    }
                    reader.Close();
                }

                conn.Close();

            }
            return list;

        }

        public List<Product> GetProducts1(int msc)
        {
            List<Product> list = new List<Product>();

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                string str = "select * from PRODUCT where subcategory_id=@msc";
                MySqlCommand cmd = new MySqlCommand(str, conn);
                cmd.Parameters.AddWithValue("msc", msc);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product()
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            ProductName = reader["productName"].ToString(),
                        });
                    }
                    reader.Close();
                }

                conn.Close();

            }
            return list;
        }
        public List<object> DoanhThu()
        {
            List<object> list = new List<object>();

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();

                string str = "SELECT sum(total) as tong, date_format(order_date, '%Y - %m') as MonthYear FROM `order` group by MonthYear";

                MySqlCommand cmd = new MySqlCommand(str, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var ob = new { year = reader["MonthYear"].ToString(), tong = Convert.ToInt32(reader["tong"]) };
                        list.Add(ob);

                    }
                    reader.Close();
                }

                conn.Close();

            }
            return list;

        }
        public List<object> Top5BestSeller()
        {
            List<object> list = new List<object>();

            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();

                string str = "select p.productname as name1, sum(o.quantity) as SL1 " +
                    "from product p,order_detail o, product_detail pd " +
                    "where p.id=pd.product_id and o.product_detail_id=pd.id " +
                    "group by productname " +
                    "order by o.quantity DESC " +
                    "limit 5";

                MySqlCommand cmd = new MySqlCommand(str, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var ob = new { name = reader["name1"].ToString(), all = Convert.ToInt32(reader["SL1"]) };
                        list.Add(ob);

                    }
                    reader.Close();
                }

                conn.Close();

            }
            return list;

        }
    }
}

